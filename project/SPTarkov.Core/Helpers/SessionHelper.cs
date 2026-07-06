using Microsoft.Extensions.Logging;
using SPTarkov.Core.Configuration;
using SPTarkov.Core.SPT;
using SPTarkov.Core.SPT.Requests;
using SPTarkov.Core.SPT.Responses;

namespace SPTarkov.Core.Helpers;

public enum ConnectStep
{
    None,
    Ping,
    Profiles,
    Types,
}

// Outcome of a SessionHelper.ConnectToServerAsync attempt.
public record ConnectResult
{
    public bool Success { get; init; }
    public ConnectStep FailedStep { get; init; } = ConnectStep.None;
    public bool TimedOut { get; init; }

    public static ConnectResult Ok()
    {
        return new ConnectResult { Success = true };
    }

    public static ConnectResult Fail(ConnectStep step, bool timedOut = false)
    {
        return new ConnectResult
        {
            Success = false,
            FailedStep = step,
            TimedOut = timedOut,
        };
    }
}

public class SessionHelper(ILogger<SessionHelper> logger, HttpHelper httpHelper, StateHelper stateHelper)
{
    private static readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(10);

    // Selects the server, then pings it and fetches its profile list and profile types, storing them on StateHelper. Failures are reported
    // via the returned ConnectResult, not thrown. On failure, partial state is left in place for the caller to clean up.
    public async Task<ConnectResult> ConnectToServerAsync(
        Server server,
        IProgress<ConnectStep>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        stateHelper.SetSelectedServer(server);

        var step = ConnectStep.Ping;
        try
        {
            progress?.Report(ConnectStep.Ping);
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                cts.CancelAfter(_requestTimeout);
                var ping = await httpHelper.GameServerGet<SPTPingResponse>(Urls.Ping, cts.Token);
                if (ping?.Response != "Pong!")
                {
                    return ConnectResult.Fail(ConnectStep.Ping);
                }
            }

            step = ConnectStep.Profiles;
            progress?.Report(ConnectStep.Profiles);
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                cts.CancelAfter(_requestTimeout);
                var profiles = await httpHelper.GameServerGet<SPTProfilesResponse>(Urls.Profiles, cts.Token);
                stateHelper.ProfileList = profiles?.Response!;
            }

            step = ConnectStep.Types;
            progress?.Report(ConnectStep.Types);
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                cts.CancelAfter(_requestTimeout);
                var types = await httpHelper.GameServerGet<SPTTypesResponse>(Urls.Types, cts.Token);
                stateHelper.ProfileTypes = types?.Response!;
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("ConnectToServerAsync timed out at step {Step}", step);
            return ConnectResult.Fail(step, timedOut: true);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("ConnectToServerAsync cancelled at step {Step}", step);
            return ConnectResult.Fail(step);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ConnectToServerAsync failed at step {Step}", step);
            return ConnectResult.Fail(step);
        }

        return ConnectResult.Ok();
    }

    // Logs into an existing profile by username and, on success, sets it as the selected profile.
    public async Task<bool> LoginToProfileAsync(MiniProfile profile, CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_requestTimeout);

            var request = new SPTLoginRequest { Username = profile.Username };
            var response = await httpHelper.GameServerPut<SPTLoginResponse>(Urls.Login, request, cts.Token);
            if (response?.Response != true)
            {
                return false;
            }

            stateHelper.SetSelectedProfile(profile);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "LoginToProfileAsync failed for {Username}", profile.Username);
            return false;
        }
    }

    // Authenticates by username, fetches the resulting profile, and sets it as the selected profile.
    public async Task<MiniProfile?> LoginWithDetailsAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_requestTimeout);

            var request = new SPTLoginRequest { Username = username };
            var login = await httpHelper.GameServerPut<SPTLoginResponse>(Urls.Login, request, cts.Token);
            if (login?.Response != true)
            {
                return null;
            }

            var profile = await httpHelper.GameServerPut<SPTProfileResponse>(Urls.Profile, request, cts.Token);
            if (profile?.Response is null)
            {
                return null;
            }

            stateHelper.SetSelectedProfile(profile.Response);
            return profile.Response;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "LoginWithDetailsAsync failed for {Username}", username);
            return null;
        }
    }
}

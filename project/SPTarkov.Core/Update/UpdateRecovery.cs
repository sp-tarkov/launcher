using Microsoft.Extensions.Logging;

namespace SPTarkov.Core.Update;

/// <summary>Completes or rolls back an interrupted update, best-effort.</summary>
public class UpdateRecovery(ILogger<UpdateRecovery> logger, UpdateTransaction transaction)
{
    public string? Run()
    {
        try
        {
            var state = transaction.ReadState();
            if (state is null)
            {
                if (!transaction.HasLeftovers())
                {
                    return null;
                }

                logger.LogWarning("Update leftovers found without a readable marker; skipping to cleanup.");
                state = new UpdateState { Version = string.Empty, Phase = UpdatePhase.Cleanup };
            }
            else
            {
                logger.LogInformation("Update marker found: version {Version}, phase {Phase}.", state.Version, state.Phase);
            }

            string? appliedVersion = null;

            switch (state.Phase)
            {
                case UpdatePhase.Staged:
                case UpdatePhase.Cleanup:
                    break;

                case UpdatePhase.Committing when transaction.SwapCompleted():
                case UpdatePhase.Relaunch:
                    logger.LogInformation("The update to {Version} completed its swap; keeping it.", state.Version);
                    transaction.RollForward();
                    appliedVersion = state.Version;
                    break;

                case UpdatePhase.Committing:
                    logger.LogWarning("The update to {Version} was interrupted mid-commit; rolling it back.", state.Version);
                    transaction.RollBack();
                    break;
            }

            transaction.Cleanup(state);
            return appliedVersion;
        }
        catch (Exception ex)
        {
            logger.LogError("Update recovery failed: {Exception}", ex);
            return null;
        }
    }
}

using SPTarkov.Core.Helpers;
using SPTarkov.Core.Mods;

namespace SPTarkov.Launcher.Helpers;

public record BlockedDependency(string Name, string Detail);

public static class DependencyDisplay
{
    // Formats a blocked dependency resolution into one name/detail item per problem.
    public static List<BlockedDependency> BuildBlockedItems(DependencyResolution resolution, LocaleHelper localeHelper)
    {
        var items = new List<BlockedDependency>();

        foreach (var group in resolution.Conflicted.GroupBy(x => x.GUID ?? x.Name))
        {
            var versions = string.Join(", ", group.Select(x => x.LatestCompatibleVersion?.Version?.ToString() ?? "?").Distinct());
            items.Add(new BlockedDependency(group.First().Name, string.Format(localeHelper.Get("download_deps_conflict_detail"), versions)));
        }

        foreach (var name in resolution.Unresolvable.Select(x => x.Name).Distinct())
        {
            items.Add(new BlockedDependency(name, localeHelper.Get("download_deps_unresolvable_detail")));
        }

        foreach (var violation in resolution.DependentViolations)
        {
            var dependents = violation.Dependents.Count > 0 ? string.Join(", ", violation.Dependents) : "?";
            var detail = violation.RequiredVersion is { } required
                ? string.Format(localeHelper.Get("download_deps_dependent_detail"), required, dependents)
                : localeHelper.Get("download_deps_unresolvable_detail");
            items.Add(new BlockedDependency(violation.Name, detail));
        }

        return items;
    }
}

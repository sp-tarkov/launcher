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

        return items;
    }
}

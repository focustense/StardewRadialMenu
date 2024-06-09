using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System.Collections;
using System.Text;

namespace RadialMenu
{
    internal record GenericModConfigKeybindOption(
        IManifest ModManifest,
        string FieldId,
        Func<string> GetPageTitle,
        Func<string> GetSectionTitle,
        Func<string> GetFieldName,
        Func<string> GetTooltip,
        Func<IEnumerable<SButton>> GetCurrentBinding)
    {
        // Used for GMCM; not displayed in UI.
        public string UniqueDisplayName => $"{ModManifest.Name}: {UniqueFieldName}";

        public string UniqueFieldName
        {
            get
            {
                var sb = new StringBuilder();
                var pageTitle = GetPageTitle();
                if (!string.IsNullOrWhiteSpace(pageTitle))
                {
                    sb.Append(pageTitle).Append(" > ");
                }
                var sectionTitle = GetSectionTitle();
                if (!string.IsNullOrWhiteSpace(sectionTitle))
                {
                    sb.Append(sectionTitle).Append(" > ");
                }
                return sb.Append(GetFieldName()).ToString();
            }
        }

        public bool MatchesBinding(IEnumerable<SButton> otherBinding)
        {
            return GetCurrentBinding().SequenceEqual(otherBinding);
        }

        public bool MatchesBinding(Keybind otherBinding)
        {
            return MatchesBinding(otherBinding.Buttons);
        }
    }

    internal class GenericModConfigKeyBindings
    {
        public static GenericModConfigKeyBindings Load(
            IGenericModMenuConfigApi configMenu, IReflectionHelper reflectionHelper)
        {
            // The only source for this data is in GMCM's ModConfigManager, but virtually everything
            // in the mod is internal except for the specific APIs we use to register our _own_
            // config options. So we have to use a whole lot of reflection, and this could easily
            // fail if GMCM updates.
            // Perhaps a future version will open up some of the API.
            var realConfigMenu = reflectionHelper
                    .GetField<object>(configMenu, "__Target")
                    .GetValue();
            var configManager = reflectionHelper
                .GetField<object>(realConfigMenu, "ConfigManager")
                .GetValue();
            var modConfigs = reflectionHelper
                .GetMethod(configManager, "GetAll")
                .Invoke<IEnumerable<object>>();
            var allOptions = new List<GenericModConfigKeybindOption>();
            foreach (var modConfig in modConfigs)
            {
                var manifest = reflectionHelper
                    .GetProperty<IManifest>(modConfig, "ModManifest")
                    .GetValue();
                var pages = reflectionHelper
                    .GetProperty<IDictionary>(modConfig, "Pages")
                    .GetValue();
                foreach (var page in pages.Values)
                {
                    var getPageTitle = reflectionHelper
                        .GetProperty<Func<string>>(page, "PageTitle")
                        .GetValue();
                    var options = reflectionHelper
                        .GetProperty<IList>(page, "Options")
                        .GetValue()
                        .Cast<object>();
                    Func<string> getSectionTitle = () => "";
                    foreach (var option in options)
                    {
                        if (option.GetType().Name == "SectionTitleModOption")
                        {
                            getSectionTitle = reflectionHelper
                                .GetProperty<Func<string>>(option, "Name")
                                .GetValue();
                            continue;
                        }
                        else if (!IsKeybindOption(option))
                        {
                            continue;
                        }
                        var fieldId = reflectionHelper
                            .GetProperty<string>(option, "FieldId")
                            .GetValue();
                        var getFieldName = reflectionHelper
                            .GetProperty<Func<string>>(option, "Name")
                            .GetValue();
                        var getTooltip = reflectionHelper
                            .GetProperty<Func<string>>(option, "Tooltip")
                            .GetValue();
                        var type = reflectionHelper
                            .GetProperty<Type>(option, "Type")
                            .GetValue();
                        Func<IEnumerable<SButton>> getValue = type.Name switch
                        {
                            "SButton" => () =>
                                [reflectionHelper.GetProperty<SButton>(option, "Value").GetValue()],
                            "KeybindList" => () =>
                                reflectionHelper
                                    .GetProperty<KeybindList>(option, "Value")
                                    .GetValue()
                                    .Keybinds
                                    .Where(kb => kb.IsBound)
                                    .FirstOrDefault()?
                                    .Buttons ?? [],
                            _ => () => []
                        };
                        allOptions.Add(new(
                            manifest,
                            fieldId,
                            getPageTitle,
                            getSectionTitle,
                            getFieldName,
                            getTooltip,
                            getValue));
                    }
                }
            }
            return new(allOptions);
        }

        public IReadOnlyList<GenericModConfigKeybindOption> AllOptions { get; }

        private readonly ILookup<(string, string), GenericModConfigKeybindOption>
            optionsByModAndFieldName =
                EmptyLookup<(string, string), GenericModConfigKeybindOption>();
        private readonly Dictionary<(string, string), GenericModConfigKeybindOption>
            optionsByModAndFieldId = [];

        private GenericModConfigKeyBindings(IReadOnlyList<GenericModConfigKeybindOption> allOptions)
        {
            AllOptions = allOptions;
            optionsByModAndFieldId = allOptions.ToDictionary(
                opt => (opt.ModManifest.UniqueID, opt.FieldId));
            optionsByModAndFieldName = allOptions.ToLookup(
                opt => (opt.ModManifest.UniqueID, opt.UniqueFieldName));
        }

        public GenericModConfigKeybindOption? Find(
            string modId, string fieldId, string fieldName, Keybind previousBinding)
        {
            var nameMatches = optionsByModAndFieldName[(modId, fieldName)];
            GenericModConfigKeybindOption? bestNameMatch = null;
            if (!string.IsNullOrEmpty(fieldName))
            {
                foreach (var nameMatch in nameMatches)
                {
                    if (bestNameMatch is null)
                    {
                        bestNameMatch = nameMatch;
                    }
                    else if (nameMatch.FieldId == fieldId
                        || (!bestNameMatch.MatchesBinding(previousBinding)
                            && nameMatch.MatchesBinding(previousBinding)))
                    {
                        bestNameMatch = nameMatch;
                        break;
                    }
                }
            }
            return bestNameMatch
                ?? optionsByModAndFieldId.GetValueOrDefault((modId, fieldId))
                // Falling back to exclusively binding-based matching should be unusual, but in the
                // event that it does become necessary, we have to remember that keybindings can be
                // changed at any point while the game is running, so unlike the field ID/name
                // lookups, we don't want to aggressively cache these, otherwise we need a system
                // for keeping the dictionary keys up to date.
                //
                // Since this is pretty rare, and since lookups shouldn't happen that often, and
                // even in a "heavy" loadout there should be a few dozen or maybe a few hundred mods
                // registered, we can just suffer the O(N) search in those instances. This is only
                // really intended to handle the case where a mod author has changed the names
                // around, and once the "link" is "restored", we won't continue to hit this.
                //
                // Of course, if a match is impossible and the link is never re-established, then we
                // will in fact keep hitting this, but at some point we have to trust the user to
                // look at the warnings in the SMAPI console.
                ?? AllOptions.FirstOrDefault(opt =>
                    opt.ModManifest.UniqueID == modId
                    && opt.MatchesBinding(previousBinding));
        }

        private static ILookup<TKey, TElement> EmptyLookup<TKey, TElement>()
        {
            return Enumerable.Empty<TKey>().ToLookup(key => key, _ => default(TElement)!);
        }

        private static bool IsKeybindOption(object option)
        {
            var optionType = option.GetType();
            if (!optionType.IsGenericType || optionType.Name != "SimpleModOption`1")
            {
                return false;
            }
            var valueType = optionType.GetGenericArguments()[0];
            return valueType == typeof(SButton) || valueType == typeof(KeybindList);
        }
    }
}

using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace RadialMenu
{
    internal record GenericModConfigKeybindOption(
        IManifest ModManifest,
        string FieldId,
        Func<string> GetFieldName,
        Func<string> GetTooltip,
        Func<IEnumerable<SButton>> GetCurrentBinding)
    {
        // Used for GMCM; not displayed in UI.
        public string UniqueDisplayName => $"{ModManifest.Name}: {GetFieldName()}";
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
                var keybindOptions = reflectionHelper
                    .GetMethod(modConfig, "GetAllOptions")
                    .Invoke<IEnumerable<object>>()
                    .Where(opt => IsKeybindOption(opt.GetType()));
                foreach (var option in keybindOptions)
                {
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
                                .FirstOrDefault()?
                                .Buttons ?? [],
                        _ => () => []
                    };
                    allOptions.Add(new(manifest, fieldId, getFieldName, getTooltip, getValue));
                }
            }
            return new(allOptions);
        }

        public IReadOnlyList<GenericModConfigKeybindOption> AllOptions { get; }

        private GenericModConfigKeyBindings(IReadOnlyList<GenericModConfigKeybindOption> allOptions)
        {
            AllOptions = allOptions;
        }

        private static bool IsKeybindOption(Type optionType)
        {
            if (!optionType.IsGenericType || optionType.Name != "SimpleModOption`1")
            {
                return false;
            }
            var valueType = optionType.GetGenericArguments()[0];
            return valueType == typeof(SButton) || valueType == typeof(KeybindList);
        }
    }
}

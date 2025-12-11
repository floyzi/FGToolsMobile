using MelonLoader;
using NOTFGT.FLZ_Common.Config.Entries.Configs;
using static NOTFGT.FLZ_Common.Config.Entries.EntriesManager;

namespace NOTFGT.FLZ_Common.Config.Entries
{
    internal class MenuEntry(MenuEntry.Type type, string id, CategoryData category, string displayName, string desc, Func<object, object> setter = null, Action postSet = null, IEntryConfig additionalConfig = null)
    {
        public enum Type
        {
            Toggle,
            InputField,
            Slider,
            Button
        }

        /// <summary>
        /// ID of the entry, should be unique for each entry.
        /// </summary>
        internal string ID { get; private set; } = id;

        /// <summary>
        /// Category where element will be placed on UI.
        /// </summary>
        internal CategoryData Category { get; private set; } = category;

        /// <summary>
        /// Name of element that will be shown on UI.
        /// Represents ID of localized string
        /// </summary>
        internal string DisplayName { get; private set; } = displayName;

        /// <summary>
        /// Description of element that will be shown on UI.
        /// Represents ID of localized string
        /// </summary>
        internal string Description { get; private set; } = desc;

        /// <summary>
        /// Type of entry, indicates how this entry will be shown on UI
        /// </summary>
        internal Type EntryType { get; private set; } = type;

        /// <summary>
        /// Additional properties of entry.
        /// </summary>
        internal IEntryConfig AdditionalConfig => additionalConfig;

        /// <summary>
        /// Used to set and get updated value of field that attached to this entry
        /// </summary>
        internal Func<object, object> Setter => setter;

        /// <summary>
        /// Initial field value of field. Null if field is not specefied
        /// </summary>
        internal object InitialValue = setter?.Invoke(null);

        /// <summary>
        /// Action that will be invoked after Setter 
        /// </summary>
        internal Action PostSetAction = postSet;

        /// <summary>
        /// Indicates can be entry included in config save or no
        /// </summary>
        internal bool CanBeSaved
        {
            get
            {
                if (EntryType == Type.Button || string.IsNullOrEmpty(ID))
                    return false;

                if (AdditionalConfig != null)
                {
                    if (AdditionalConfig is BaseEntryConfig baseConf && baseConf.SaveInConfigCondition != null)
                        return baseConf.SaveInConfigCondition();
                }

                return true;
            }
        }
        /// <summary>
        /// Action that will be invoked after entry value is changed
        /// </summary>
        internal Action<object> OnEntryChanged;

        internal object GetValue() => setter?.Invoke(null);

        internal void SetValue(object val)
        {
            try
            {
                Setter?.Invoke(val);
                PostSetAction?.Invoke();
                OnEntryChanged?.Invoke(val);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Exception on Set on entry {ID} ({EntryType})!\n{ex}");
            }
        }
    }
}

using System;
using System.Text;
using System.Collections.Generic;
using FMOD.Studio;
using IL.Celeste.Mod;

namespace Celeste.Mod.InGameDigitalClock;


public class SettingCondition
{

    private string currentStringValue { get; set; }
    private string requiredStringValue { get; }
    public bool isConditionMet { get; set; }


    public SettingCondition(string currentStringValue, string requiredStringValue)
    {
        this.currentStringValue = currentStringValue;
        this.requiredStringValue = requiredStringValue;
        this.isConditionMet = (currentStringValue == requiredStringValue);
    }


    // Called whenever currentStringValue needs to be updated
    public void UpdateConditionMetBool(string newCurrentValue)
    {
        currentStringValue = newCurrentValue;
        isConditionMet = (currentStringValue == requiredStringValue);
    }
}


public class ModSetting
{

    private static int menuCursorIndex;
    private TextMenu.Item menuItem { get; }
    private bool isInMenu { get; set; } = false;
    public List<ModSetting> subSettingsList { get; set; } = new List<ModSetting> { };
    public List<SettingCondition> conditionsList { get; } = new List<SettingCondition> { };


    public ModSetting(TextMenu.Item menuItem, List<SettingCondition> conditionsList)
    {
        this.menuItem = menuItem;
        this.conditionsList = conditionsList;
    }


    // Called when adding master settings, skips condition checks
    public void AddItem(TextMenu menu)
    {
        if (!isInMenu)
        {
            menu.Add(menuItem);
            isInMenu = true;
        }
    }


    // Called to insert the sub-settings below their parent setting
    private void InsertItem(TextMenu menu, int index)
    {

        // Conditions check
        foreach (SettingCondition condition in conditionsList)
        {
            if (!condition.isConditionMet) { return; }
        }

        if (!isInMenu)
        {
            menu.Insert(index, menuItem);
            isInMenu = true;
            menuCursorIndex++;
        }
    }


    // Called from the RemoveSubSettingItem method
    private void RemoveItem(TextMenu menu)
    {
        if (isInMenu)
        {
            menu.Remove(menuItem);
            isInMenu = false;
        }
    }


    // Called from the UpdateSubSettingItem method
    public void InsertSubSettingItems(TextMenu menu)
    {

        // Initialize menuCursorIndex
        menuCursorIndex = menu.IndexOf(menuItem) + 1;

        // Insert sub-settings
        foreach (ModSetting subSetting in subSettingsList)
        {
            subSetting.InsertItem(menu, menuCursorIndex);
        }

        // Insert the sub-settings' sub-settings
        foreach (ModSetting subSetting in subSettingsList)
        {
            subSetting.InsertSubSettingItems(menu);
        }
    }


    // Called from the UpdateSubSettingItem method
    private void RemoveSubSettingItems(TextMenu menu)
    {

        // Remove the sub-settings' sub-settings, then remove the sub-settings
        foreach (ModSetting subSetting in subSettingsList)
        {
            subSetting.RemoveSubSettingItems(menu);
            subSetting.RemoveItem(menu);
        }
    }


    // Called :3
    public void UpdateSubSettingItems(TextMenu menu)
    {
        RemoveSubSettingItems(menu);
        InsertSubSettingItems(menu);
    }
}


public class InGameDigitalClockModuleSettings : EverestModuleSettings
{


    #region Mod Options
    // Mod Options
    public bool displayDigitalClock { get; set; } = true;

    // Display Mode
    public bool displaySeparately { get; set; } = true;
    public AdditionalClockOptions additionalClock { get; set; } = AdditionalClockOptions.None;
    public enum AdditionalClockOptions
    {
        None,
        Chapter,
        File
    }
    public ClockTypeOptions clockType { get; set; } = ClockTypeOptions.RealTimeAndChapterTime;
    public enum ClockTypeOptions
    {
        RealTimeAndChapterTime,
        ChapterTimeAndRealTime,
        FileTimeAndRealTime
    }

    // Clock Settings
    public bool showSeconds { get; set; } = false;
    public bool blinkColon { get; set; } = true;
    #endregion


    // Initialize masterSettingsList & settingConditionsList
    private List<ModSetting> masterSettingsList;
    private List<SettingCondition> settingConditionsList;


    public void InitializeMasterSettings(TextMenu menu)
    {

        #region Declare variables
        // Declare TextMenu.Item objects
        TextMenu.OnOff displayDigitalClockOnOff;
        TextMenu.SubHeader displayModeSubHeader;
        TextMenu.OnOff displaySeparatelyOnOff;
        TextMenu.Slider additionalClockSlider;
        TextMenu.Slider clockTypeSlider;

        TextMenu.SubHeader clockSettingsSubHeader;
        TextMenu.OnOff showSecondsOnOff;
        TextMenu.OnOff blinkColonOnOff;

        // Declare ModSetting objects
        ModSetting displayDigitalClockModSetting;
        ModSetting displayModeSubHeaderModSetting;
        ModSetting displaySeparatelyModSetting;
        ModSetting additionalClockModSetting;
        ModSetting clockTypeModSetting;

        ModSetting clockSettingsSubHeaderModSetting;
        ModSetting showSecondsModSetting;
        ModSetting blinkColonModSetting;
        #endregion


        #region Create TextMenu.Item objects
        // Display Digital Clock
        displayDigitalClockOnOff = new TextMenu.OnOff(Dialog.Clean("InGameDigitalClock_DisplayDigitalClock_Setting"), displayDigitalClock);
        displayDigitalClockOnOff.Change(enabled => { displayDigitalClock = enabled; UpdateDisplayDigitalClockSubSettings(menu); });


        // Display Mode SubHeader
        displayModeSubHeader = new TextMenu.SubHeader(Dialog.Clean("InGameDigitalClock_DisplayMode_Header"), false);

        // Display Separately
        displaySeparatelyOnOff = new TextMenu.OnOff(Dialog.Clean("InGameDigitalClock_DisplaySeparately_Setting"), displaySeparately);
        displaySeparatelyOnOff.Change(enabled => { displaySeparately = enabled; UpdateDisplaySeparatelySubSettings(menu); });

        // Additional Clock
        int additionalClockEnumInt = (int)additionalClock;
        additionalClockSlider = new TextMenu.Slider(
            Dialog.Clean("InGameDigitalClock_AdditionalClock_Setting"),
            (additionalClockEnumInt) => Dialog.Clean("InGameDigitalClock_AdditionalClock_Setting_" + Enum.GetName(typeof(AdditionalClockOptions), (AdditionalClockOptions)additionalClockEnumInt) + "_Option"),
            0, 2,
            additionalClockEnumInt);
        additionalClockSlider.Change(enumInt => { additionalClock = (AdditionalClockOptions)enumInt; });

        // Clock Type
        int clockTypeEnumInt = (int)clockType;
        clockTypeSlider = new TextMenu.Slider(
            Dialog.Clean("InGameDigitalClock_ClockType_Setting"),
            (clockTypeEnumInt) => Dialog.Clean("InGameDigitalClock_ClockType_Setting_" + Enum.GetName(typeof(ClockTypeOptions), (ClockTypeOptions)clockTypeEnumInt) + "_Option"),
            0, 2,
            clockTypeEnumInt);
        clockTypeSlider.Change(enumInt => { clockType = (ClockTypeOptions)enumInt; });


        // Clock Settings SubHeader
        clockSettingsSubHeader = new TextMenu.SubHeader(Dialog.Clean("InGameDigitalClock_ClockSettings_Header"), false);

        // Show Seconds
        showSecondsOnOff = new TextMenu.OnOff(Dialog.Clean("InGameDigitalClock_ShowSeconds_Setting"), showSeconds);
        showSecondsOnOff.Change(enabled => { showSeconds = enabled; });

        // Blink Colon
        blinkColonOnOff = new TextMenu.OnOff(Dialog.Clean("InGameDigitalClock_BlinkColon_Setting"), blinkColon);
        blinkColonOnOff.Change(enabled => { blinkColon = enabled; });
        #endregion


        // Create SettingConditions list
        settingConditionsList = new List<SettingCondition> {
            new SettingCondition(displayDigitalClock.ToString(), "True"),
            new SettingCondition(displaySeparately.ToString(), "True"),
            new SettingCondition(displaySeparately.ToString(), "False"),
        };


        #region Create ModSetting objects
        // Display Digital Clock
        displayDigitalClockModSetting = new ModSetting(
            displayDigitalClockOnOff,
            new List<SettingCondition> { }
        );

        // Display Mode SubHeader
        displayModeSubHeaderModSetting = new ModSetting(
            displayModeSubHeader,
            new List<SettingCondition> { settingConditionsList[0] }
        );

        // Display Separately
        displaySeparatelyModSetting = new ModSetting(
            displaySeparatelyOnOff,
            new List<SettingCondition> { settingConditionsList[0] }
        );

        // Additional Clock
        additionalClockModSetting = new ModSetting(
            additionalClockSlider,
            new List<SettingCondition> { settingConditionsList[0], settingConditionsList[1] }
        );

        // Clock Type
        clockTypeModSetting = new ModSetting(
            clockTypeSlider,
            new List<SettingCondition> { settingConditionsList[0], settingConditionsList[2] }
        );

        // Clock Settings SubHeader
        clockSettingsSubHeaderModSetting = new ModSetting(
            clockSettingsSubHeader,
            new List<SettingCondition> { settingConditionsList[0] }
        );

        // Show Seconds
        showSecondsModSetting = new ModSetting(
            showSecondsOnOff,
            new List<SettingCondition> { settingConditionsList[0] }
        );

        // Blink Colon
        blinkColonModSetting = new ModSetting(
            blinkColonOnOff,
            new List<SettingCondition> { settingConditionsList[0] }
        );
        #endregion


        #region Create & Add sub-settings to ModSetting objects
        // DisplayDigitalClock sub-settings list
        List<ModSetting> displayDigitalClockSubSettingsList = new List<ModSetting> {
            displayModeSubHeaderModSetting, // SubHeader
            displaySeparatelyModSetting,
            clockSettingsSubHeaderModSetting, // SubHeader
            showSecondsModSetting,
            blinkColonModSetting
        };

        // DisplaySeparately sub-settings list
        List<ModSetting> displaySeparatelySubSettingsList = new List<ModSetting> {
            additionalClockModSetting,
            clockTypeModSetting
        };

        // Add sub-settings lists to the ModSetting objects
        displayDigitalClockModSetting.subSettingsList = displayDigitalClockSubSettingsList;
        displaySeparatelyModSetting.subSettingsList = displaySeparatelySubSettingsList;
        #endregion


        #region Set masterSettingsList to contain the master settings
        masterSettingsList = new List<ModSetting> { displayDigitalClockModSetting };
        #endregion
    }


    // Called by Everest from the main module
    public void CreateModMenuSection(TextMenu menu)
    {

        InitializeMasterSettings(menu);

        // Add master settings
        foreach (ModSetting setting in masterSettingsList)
        {
            setting.AddItem(menu);
        }

        // Add the master settings's sub-settings
        foreach (ModSetting setting in masterSettingsList)
        {
            setting.InsertSubSettingItems(menu);
        }
    }


    // Called when the 'Display Digital Clock' option is changed
    public void UpdateDisplayDigitalClockSubSettings(TextMenu menu)
    {
        // Update 'Display Digital Clock' setting condition
        settingConditionsList[0].UpdateConditionMetBool(displayDigitalClock.ToString());

        // Update related sub-settings
        masterSettingsList[0].UpdateSubSettingItems(menu);
    }


    // Called when the 'Display Separately' option is changed
    public void UpdateDisplaySeparatelySubSettings(TextMenu menu)
    {
        // Update 'Display Separately' setting condition
        settingConditionsList[1].UpdateConditionMetBool(displaySeparately.ToString());
        settingConditionsList[2].UpdateConditionMetBool(displaySeparately.ToString());

        // Update related sub-settings
        masterSettingsList[0].subSettingsList[1].UpdateSubSettingItems(menu);
    }
}

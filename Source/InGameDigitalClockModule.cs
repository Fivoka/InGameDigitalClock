using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Celeste;
using Monocle;
using FMOD.Studio;


namespace Celeste.Mod.InGameDigitalClock;


public class InGameDigitalClockModule : EverestModule {

    // Everest magic
    public static InGameDigitalClockModule Instance { get; private set; }
    public override Type SettingsType => typeof(InGameDigitalClockModuleSettings);
    public static InGameDigitalClockModuleSettings Settings => (InGameDigitalClockModuleSettings)Instance._Settings;


    public InGameDigitalClockModule() {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(InGameDigitalClockModule), LogLevel.Verbose);
#else
        Logger.SetLogLevel(nameof(InGameDigitalClockModule), LogLevel.Info);
#endif
    }


    // Mod options
    public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot) {
        CreateModMenuSectionHeader(menu, inGame, snapshot);
        Settings.CreateModMenuSection(menu);
    }


    // Load & Unload
    public override void Load() {
        On.Celeste.SpeedrunTimerDisplay.Render += modSpeedrunTimerDisplayRender;
        On.Celeste.TotalStrawberriesDisplay.Update += modTotalStrawberriesDisplayUpdate;
    }

    public override void Unload() {
        On.Celeste.SpeedrunTimerDisplay.Render -= modSpeedrunTimerDisplayRender;
        On.Celeste.TotalStrawberriesDisplay.Update -= modTotalStrawberriesDisplayUpdate;
    }


    // Speedrun timer display render method
    private void modSpeedrunTimerDisplayRender(On.Celeste.SpeedrunTimerDisplay.orig_Render orig, SpeedrunTimerDisplay self) {

        if (Settings.displayDigitalClock)
        {
            Scene baseScene = self.Scene;
            MTexture bg = self.bg;
            Wiggler wiggler = self.wiggler;
            Level level = baseScene as Level;
            Session session = level.Session;

            float xPos = 0f;
            float yPos = self.Y;
            float hideSecondsXOffset = -55f;

            if (Settings.displaySeparately)
            {
                // Real Time Clock
                float xOffset = Settings.showSeconds ? 0 : hideSecondsXOffset;

                bg.Draw(new Vector2(xPos + xOffset, yPos));
                DrawRealTime(new Vector2(xPos + 32f, yPos + 44f), getTimeString(Settings.showSeconds));

                yPos += 65f;

                if (Settings.additionalClock == InGameDigitalClockModuleSettings.AdditionalClockOptions.Chapter)
                {
                    // Separate Chapter Clock
                    string chapterTimeString = TimeSpan.FromTicks(session.Time).ShortGameplayFormat();

                    bg.Draw(new Vector2(xPos, yPos));
                    SpeedrunTimerDisplay.DrawTime(new Vector2(xPos + 32f, yPos + 44f), chapterTimeString, 1f + wiggler.Value * 0.15f, session.StartedFromBeginning, level.Completed, session.BeatBestTime);
                }
                else if (Settings.additionalClock == InGameDigitalClockModuleSettings.AdditionalClockOptions.File)
                {
                    // Separate File Clock
                    TimeSpan fileTimeSpan = TimeSpan.FromTicks(SaveData.Instance.Time);
                    int fileTime = (int)fileTimeSpan.TotalHours;
                    int extraWidth = ((fileTime < 10) ? 64 : ((fileTime < 100) ? 96 : 128));
                    string fileTimeString = fileTime + fileTimeSpan.ToString("\\:mm\\:ss\\.fff");
                    TimeSpan chapterTimeSpan = TimeSpan.FromTicks(session.Time);
                    string chapterTimeString = ((!(chapterTimeSpan.TotalHours >= 1.0)) ? chapterTimeSpan.ToString("mm\\:ss") : ((int)chapterTimeSpan.TotalHours + ":" + chapterTimeSpan.ToString("mm\\:ss")));
                        
                    Draw.Rect(0, yPos, extraWidth + 2, 38f, Color.Black);
                    bg.Draw(new Vector2(xPos + (float)extraWidth, yPos));
                    SpeedrunTimerDisplay.DrawTime(new Vector2(xPos + 32f, yPos + 44f), fileTimeString);

                    bg.Draw(new Vector2(xPos, yPos + 38f), Vector2.Zero, Color.White, 0.6f);
                    SpeedrunTimerDisplay.DrawTime(new Vector2(xPos + 32f, yPos + 40f + 26.400002f), chapterTimeString, (1f + wiggler.Value * 0.15f) * 0.6f, session.StartedFromBeginning, level.Completed, session.BeatBestTime, 0.6f);
                }
            }
            else
            {
                if (Settings.clockType == InGameDigitalClockModuleSettings.ClockTypeOptions.RealTimeAndChapterTime)
                {
                    // Real Time & Chapter Time Clock
                    float xOffset = Settings.showSeconds ? 0 : hideSecondsXOffset;
                    TimeSpan chapterTimeSpan = TimeSpan.FromTicks(session.Time);
                    string chapterTimeString = ((!(chapterTimeSpan.TotalHours >= 1.0)) ? chapterTimeSpan.ToString("mm\\:ss") : ((int)chapterTimeSpan.TotalHours + ":" + chapterTimeSpan.ToString("mm\\:ss")));

                    bg.Draw(new Vector2(xPos + xOffset, yPos));
                    DrawRealTime(new Vector2(xPos + 32f, yPos + 44f), getTimeString(Settings.showSeconds));

                    bg.Draw(new Vector2(xPos, yPos + 38f), Vector2.Zero, Color.Black, 0.6f);
                    SpeedrunTimerDisplay.DrawTime(new Vector2(xPos + 32f, yPos + 40f + 26.400002f), chapterTimeString, (1f + wiggler.Value * 0.15f) * 0.6f, session.StartedFromBeginning, level.Completed, session.BeatBestTime, 0.6f);
                }
                else if (Settings.clockType == InGameDigitalClockModuleSettings.ClockTypeOptions.ChapterTimeAndRealTime)
                {
                    // Chapter Time & Real Time Clock
                    string chapterTimeString = TimeSpan.FromTicks(session.Time).ShortGameplayFormat();

                    bg.Draw(new Vector2(xPos, yPos));
                    SpeedrunTimerDisplay.DrawTime(new Vector2(xPos + 32f, yPos + 44f), chapterTimeString, 1f + wiggler.Value * 0.15f, session.StartedFromBeginning, level.Completed, session.BeatBestTime);

                    bg.Draw(new Vector2(xPos, yPos + 38f), Vector2.Zero, Color.Black, 0.6f);
                    DrawRealTime(new Vector2(xPos + 32f, yPos + 40f + 26.400002f), getTimeString(false), 0.6f, 0.6f);
                }
                else
                {
                    // File Time & Real Time Clock
                    TimeSpan fileTimeSpan = TimeSpan.FromTicks(SaveData.Instance.Time);
                    int fileTime = (int)fileTimeSpan.TotalHours;
                    int extraWidth = ((fileTime < 10) ? 64 : ((fileTime < 100) ? 96 : 128));
                    string fileTimeString = fileTime + fileTimeSpan.ToString("\\:mm\\:ss\\.fff");

                    Draw.Rect(xPos, yPos, extraWidth + 2, 38f, Color.Black);
                    bg.Draw(new Vector2(xPos + (float)extraWidth, yPos));
                    SpeedrunTimerDisplay.DrawTime(new Vector2(xPos + 32f, yPos + 44f), fileTimeString);

                    bg.Draw(new Vector2(xPos, yPos + 38f), Vector2.Zero, Color.Black, 0.6f);
                    DrawRealTime(new Vector2(xPos + 32f, yPos + 40f + 26.400002f), getTimeString(false), 0.6f, 0.6f);
                }
            }
        }
        else
        {
            orig(self);
        }
    }


    private void DrawRealTime(Vector2 position, string timeString, float baseScale = 1f, float alpha = 1f)
    {
        float numberWidth = SpeedrunTimerDisplay.numberWidth;
        float spacerWidth = SpeedrunTimerDisplay.spacerWidth;

        PixelFont font = Dialog.Languages["english"].Font;
        float fontFaceSize = Dialog.Languages["english"].FontFaceSize;

        float scale = baseScale;
        float xPos = position.X;
        float yPos = position.Y;

        DateTime date = DateTime.Now;
        int second = date.Second;

        Color color1 = Color.White * alpha;
        Color color2 = Color.Gray * alpha;

        for (int i = 0; i < timeString.Length; i++)
        {
            if (i == 5)
            {
                scale = baseScale * 0.7f;
                yPos -= 5f * baseScale;
            }

            Color textColor = ((scale < baseScale) ? color2 : color1);
            if (i == 2 && Settings.blinkColon && second % 2 == 1) { textColor = color1 * 0.3f; }

            char c = timeString[i];
            float charWidth = (((c == ':' || c == ' ') ? spacerWidth : numberWidth) + 4f) * scale;
            font.DrawOutline(fontFaceSize, c.ToString(), new Vector2(xPos + charWidth / 2f, yPos), new Vector2(0.5f, 1f), Vector2.One * scale, textColor, 2f, Color.Black);
            xPos += charWidth;
        }
    }


    private string getTimeString(bool includeSeconds)
    {
        DateTime date = DateTime.Now;

        string text = "";

        string hourText = (date.Hour.ToString().Length == 1 ? "0" + date.Hour.ToString() : date.Hour.ToString());
        string minuteText = (date.Minute.ToString().Length == 1 ? "0" + date.Minute.ToString() : date.Minute.ToString());

        text += $"{hourText}:{minuteText}";

        if (includeSeconds)
        {
            string secondText = (date.Second.ToString().Length == 1 ? "0" + date.Second.ToString() : date.Second.ToString());
            text += $":{secondText}";
        }

        return text;
    }


    // Total strawberries display update method
    private void modTotalStrawberriesDisplayUpdate(On.Celeste.TotalStrawberriesDisplay.orig_Update orig, TotalStrawberriesDisplay self)
    {
        orig(self);

        if (Settings.displayDigitalClock && self.Visible)
        {
            float yPos = 96f;

            Level level = self.Scene as Level;
            if (!level.TimerHidden) // <----- Try without this if
            {
                if (Settings.displaySeparately)
                {
                    yPos += 58f;

                    if (Settings.additionalClock == InGameDigitalClockModuleSettings.AdditionalClockOptions.Chapter)
                    {
                        yPos += 65f;
                    }
                    else if (Settings.additionalClock == InGameDigitalClockModuleSettings.AdditionalClockOptions.File)
                    {
                        yPos += 85f;
                    }
                }
                else
                {
                    yPos += 78f;
                }
            }
            self.Y = yPos;
            //self.Y = Calc.Approach(self.Y, yPos, Engine.DeltaTime * 800f);
        }
    }
}

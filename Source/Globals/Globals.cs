public enum Edition { Lite }

public static class Globals
{
    public static Edition EDITION = Edition.Lite;

    public static string VERSION = "7.29.139.8317"; // updated by Version.bat (with AssemblyInfo.cs of all projects)
    public static string RELEASE = "AAU";
    public static string SHORT_VERSION
    {
        get
        {
            string version = VERSION;
            string[] parts = version.Split('.');
            if (parts.Length == 4)
            {
                int pos = version.LastIndexOf('.');
                if (pos > -1)
                {
                    version = version.Substring(0, pos);
                }
            }

            if (EDITION == Edition.Lite)
            {
                return ("" + version);
            }
            else
            {
                return ("!" + version); // Invalid Edition
            }
        }
    }
    public static string LONG_VERSION
    {
        get
        {
            if (EDITION == Edition.Lite)
            {
                return ("" + EDITION);
            }
            else
            {
                return ("!" + EDITION); // Invalid Edition
            }
        }
    }

    // Global Variables
    public static string DELIMITER = "\t";
    public static string SUB_DELIMITER = "|";
    public static string DATE_FORMAT = "yyyy-MM-dd";
    public static string TIME_FORMAT = "HH:mm:ss";
    public static string DATETIME_FORMAT = DATE_FORMAT + " " + TIME_FORMAT;
    public static string NUMBER_FORMAT = "000";
    public static int MAX_NUMBERS = int.MaxValue;

    // Global Folders
    public static string LANGUAGES_FOLDER = "Languages";
    public static string LANGUAGES_FLAGS_FOLDER = "Languages/Flags";
    public static string USERTEXT_FOLDER = "UserText";
    public static string TRANSLATIONS_FOLDER = "Translations";
    public static string TRANSLATIONS_OFFLINE_FOLDER = "Translations/Offline";
    public static string TRANSLATIONS_FLAGS_FOLDER = "Translations/Flags";
    public static string IMAGES_FOLDER = "Images";
    public static string FONTS_FOLDER = "Fonts";
    public static string DATA_FOLDER = "Data";
    public static string AUDIO_FOLDER = "Audio";
    public static string EXTERNAL_AUDIO_FOLDER = "D:/Quran/Audio";
    public static string RULES_FOLDER = "Rules";
    public static string VALUES_FOLDER = "Values";
    public static string NUMBERS_FOLDER = "Numbers";
    public static string STATISTICS_FOLDER = "Statistics";
    public static string DRAWINGS_FOLDER = "Drawings";
    public static string BOOKMARKS_FOLDER = "Bookmarks";
    public static string HISTORY_FOLDER = "History";
    public static string HELP_FOLDER = "Help";
}

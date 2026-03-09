namespace ProjectHD
{
    public static class Global
    {
        public static DataManager DataManager;

        static Global()
        {
            DataManager = new();
        }
    }
}
namespace Core.Enums
{
    public enum UpdateSettingsStatus
    {
        Ok = 0,
        JsonFormarIncorrrect = 1,
        OutOfDate = 2,
        InternalError = 3,
        NotFound = 4,
        HasDuplicated = 5,
        AlreadyExists = 6,
        InvalidInput = 7,
        InvalidRequest = 8,
        GitDownloadError = 9,
        YamlProcessingError = 10,
    }
}

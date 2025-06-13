namespace Application.Interfaces
{
    public interface ITranslationService
    {
        Task<string> TranslateAsync(string key, string languageCode);
    }
}


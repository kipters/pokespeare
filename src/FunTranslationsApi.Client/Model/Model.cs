namespace FunTranslationsApi.Client.Model
{
    public record TranslationResult(Successes Success, TranslationContent Contents);
    public record Successes(int Total);
    public record TranslationContent(string Translated, string Text, string Translation);
}

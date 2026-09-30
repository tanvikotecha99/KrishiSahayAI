using System.Threading.Tasks;

namespace KrishiSahayAI.Services
{
    public interface IGeminiService
    {
        // =========================================================
        // TEXT AI
        // =========================================================

        Task<string> AskAsync(
            string userQuestion,
            string farmContext);

        // =========================================================
        // FARM BUDGET
        // =========================================================

        Task<string> AllocateFarmBudgetAsync(
            string farmContext);

        // =========================================================
        // MULTIMODAL CROP DOCTOR
        // =========================================================

        Task<string> AnalyzeCropImageAsync(
            string observation,
            string farmContext,
            byte[] imageBytes,
            string mimeType);

        // =========================================================
        // GEMINI SPEECH-TO-TEXT
        // =========================================================

        Task<SpeechTranscriptionResult> TranscribeAudioAsync(
            byte[] audioBytes,
            string mimeType);
    }

    public sealed class SpeechTranscriptionResult
    {
        public string Transcript { get; init; } = "";
        public string LanguageCode { get; init; } = "en";
    }
}

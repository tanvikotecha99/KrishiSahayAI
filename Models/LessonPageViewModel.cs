namespace KrishiSahayAI.Models
{
    public class LessonPageViewModel
    {
        public LearningTopic Topic { get; set; } = new();

        public List<LearningLesson> Lessons { get; set; } = new();
    }
}
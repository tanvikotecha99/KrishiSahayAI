namespace KrishiSahayAI.Models
{
    public class LearningTopic
    {
        public int Id { get; set; }

        public string Title { get; set; } = "";

        public string Description { get; set; } = "";

        public string Icon { get; set; } = "";

        public string Category { get; set; } = "";

        public int Order { get; set; }

        public int LessonCount { get; set; }
    }
}
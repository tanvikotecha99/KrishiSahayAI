namespace KrishiSahayAI.Models
{
    public class LearningLesson
    {
        public int Id { get; set; }

        public int TopicId { get; set; }

        public string Title { get; set; } = "";

        public string Introduction { get; set; } = "";

        public string Content { get; set; } = "";

        public string KeyPoint { get; set; } = "";

        public int Order { get; set; }
    }
}
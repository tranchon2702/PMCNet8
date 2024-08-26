
namespace PMCNet8.Models
{
    public class QuestionViewModel
    {
        public long QuestionId { get; set; }
        public string QuestionTitle { get; set; }
        public string Type { get; set; }
        public List<OptionViewModel> Options { get; set; }
        public List<ResponseViewModel> Responses { get; set; }
        public string Statistics { get; set; }
        public int Order { get;  set; }

        public string DisplayOrder => $"Câu hỏi {Order}";
    }
}

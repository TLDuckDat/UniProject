using UniProject.API.Modules.ProjectPeriod;

namespace UniProject.API.Modules.Grading
{
    public record GradeOutcome(decimal FinalScore, GradeRank Rank, FinalResult Result);

    public interface IProjectGradeCalculator
    {
        /// <summary>Trả về null nếu còn thiếu cột điểm bắt buộc (cột có trọng số &gt; 0) của loại đồ án.</summary>
        GradeOutcome? Calculate(GradeWeights weights, PassRules rules, decimal? advisorScore, decimal? reviewerScore, decimal? committeeScore);
    }

    public class ProjectGradeCalculator : IProjectGradeCalculator
    {
        /// <summary>
        /// Một công thức chung cho mọi loại đồ án, chỉ khác trọng số và ngưỡng đạt (lấy từ IProjectTypePolicy):
        /// điểm tổng kết = Σ (điểm cột × trọng số), làm tròn 2 chữ số.
        /// Xếp loại: ≥ 9 Xuất sắc, ≥ 8 Giỏi, ≥ 6.5 Khá, ≥ 5 Trung bình, còn lại Yếu.
        /// KHONG_DAT nếu tổng kết dưới ngưỡng đạt (mặc định 5) hoặc có cột bắt buộc dưới mức tối thiểu
        /// (mặc định 4; null = không áp dụng).
        /// </summary>
        public GradeOutcome? Calculate(GradeWeights weights, PassRules rules, decimal? advisorScore, decimal? reviewerScore, decimal? committeeScore)
        {
            var components = new[]
            {
                (Score: advisorScore, Weight: weights.Advisor),
                (Score: reviewerScore, Weight: weights.Reviewer),
                (Score: committeeScore, Weight: weights.Committee)
            }.Where(c => c.Weight > 0).ToList();

            if (components.Any(c => !c.Score.HasValue))
                return null;

            var finalScore = Math.Round(components.Sum(c => c.Score!.Value * c.Weight), 2);

            var rank = finalScore switch
            {
                >= 9.0m => GradeRank.XUAT_SAC,
                >= 8.0m => GradeRank.GIOI,
                >= 6.5m => GradeRank.KHA,
                >= 5.0m => GradeRank.TRUNG_BINH,
                _ => GradeRank.YEU
            };

            var failed = finalScore < rules.FinalPassMark
                         || (rules.ComponentPassMark.HasValue && components.Any(c => c.Score!.Value < rules.ComponentPassMark.Value));
            return new GradeOutcome(finalScore, rank, failed ? FinalResult.KHONG_DAT : FinalResult.DAT);
        }
    }
}

using Upm.Domain.Common;
using Upm.Domain.Entities;
using Upm.Domain.Enums;
using Upm.Domain.PhanQuyen;
using Upm.Domain.Rules;
using Upm.Domain.Workflow;

namespace Upm.Domain.Tests.Workflow;

public sealed class DATNWorkflowTests
{
    public static TheoryData<string> MaChuyen => new(DATNWorkflow.DinhNghia.Select(x => x.MaChuyen));

    public static TheoryData<string> MaChuyenHeThong => new(["T01", "T02", "T01R", "T02R", "T03", "T10", "T11", "T12", "T14", "T20"]);

    [Theory]
    [MemberData(nameof(MaChuyen))]
    public void Moi_T01_den_T22_co_the_thanh_cong(string maChuyen)
    {
        var definition = Definition(maChuyen);
        var entity = new SVTrongDot { Id = 51, TrangThai = definition.Tu };
        var context = Context(definition, useSystem: definition.VaiTroDuocPhep.Count == 0);

        var result = DATNWorkflow.Transition(entity, maChuyen, context);

        Assert.Equal(definition.Den, entity.TrangThai);
        Assert.Equal(definition.Tu, result.LichSu.TuTrangThai);
        Assert.Equal(definition.Den, result.LichSu.DenTrangThai);
        Assert.Equal(51, result.LichSu.SVTrongDotId);
    }

    [Theory]
    [MemberData(nameof(MaChuyen))]
    public void Moi_T01_den_T22_tu_choi_sai_quyen(string maChuyen)
    {
        var definition = Definition(maChuyen);
        var entity = new SVTrongDot { Id = 52, TrangThai = definition.Tu };
        var context = Context(definition, useSystem: false, roles: new HashSet<string> { "KhongCoQuyen" });

        var error = Assert.Throws<DomainException>(() => DATNWorkflow.Transition(entity, maChuyen, context));

        Assert.Equal("UPM_WORKFLOW_PERMISSION_DENIED", error.Code);
        Assert.Equal(definition.Tu, entity.TrangThai);
    }

    [Theory]
    [MemberData(nameof(MaChuyenHeThong))]
    public void Chuyen_he_thong_duoc_phep_cho_cac_job_da_quy_dinh(string maChuyen)
    {
        var definition = Definition(maChuyen);
        var entity = new SVTrongDot { Id = 53, TrangThai = definition.Tu };

        var result = DATNWorkflow.Transition(entity, maChuyen, Context(definition, useSystem: true));

        Assert.Equal(definition.Den, result.LichSu.DenTrangThai);
        Assert.Null(result.LichSu.NguoiThucHienId);
    }

    [Fact]
    public void Khong_the_nhay_sang_trang_thai_khong_co_trong_bang()
    {
        var entity = new SVTrongDot { Id = 54, TrangThai = TrangThaiSVTrongDot.CHO_XET };
        var context = new WorkflowContext(WorkflowActorType.NguoiDung, 1, new HashSet<string> { VaiTroMa.PhongDaoTao },
            "Lý do", null, DateTime.UtcNow, new HashSet<WorkflowFact>(), []);

        var error = Assert.Throws<DomainException>(() => DATNWorkflow.Transition(entity, TrangThaiSVTrongDot.HOAN_TAT, context));

        Assert.Equal("UPM_WORKFLOW_TRANSITION_NOT_FOUND", error.Code);
        Assert.Equal(TrangThaiSVTrongDot.CHO_XET, entity.TrangThai);
    }

    [Theory]
    [InlineData(TrangThaiSVTrongDot.PHAI_LAM_LAI)]
    [InlineData(TrangThaiSVTrongDot.KHONG_DU_DK)]
    [InlineData(TrangThaiSVTrongDot.KHONG_DU_DK_CHUYEN_THAY_THE)]
    [InlineData(TrangThaiSVTrongDot.RUT)]
    [InlineData(TrangThaiSVTrongDot.HOAN_TAT)]
    public void Trang_thai_ket_thuc_khong_co_chuyen_hop_le(TrangThaiSVTrongDot status)
    {
        var entity = new SVTrongDot { Id = 55, TrangThai = status };
        var context = new WorkflowContext(WorkflowActorType.HeThong, null, new HashSet<string>(), null, null,
            DateTime.UtcNow, new HashSet<WorkflowFact>(), []);

        Assert.Empty(DATNWorkflow.ChuyenHopLe(entity, context));
    }

    [Theory]
    [InlineData("T01R", TrangThaiSVTrongDot.KHONG_DU_DK)]
    [InlineData("T02R", TrangThaiSVTrongDot.KHONG_DU_DK_CHUYEN_THAY_THE)]
    public void A19_Xet_lai_du_dieu_kien_ve_CHO_XET(string maChuyen, TrangThaiSVTrongDot tu)
    {
        var entity = new SVTrongDot { Id = 57, TrangThai = tu };
        var context = new WorkflowContext(WorkflowActorType.NguoiDung, 1, new HashSet<string> { VaiTroMa.PhongDaoTao },
            "Xét lại", null, DateTime.UtcNow, new HashSet<WorkflowFact> { WorkflowFact.GiaoDuDieuKien }, []);

        var result = DATNWorkflow.Transition(entity, maChuyen, context);

        Assert.Equal(TrangThaiSVTrongDot.CHO_XET, entity.TrangThai);
        Assert.Equal(tu, result.LichSu.TuTrangThai);
    }

    [Theory]
    [InlineData(WorkflowFact.GiaoKhongDuDieuKien)]
    [InlineData(WorkflowFact.GiaoChuyenHocPhanThayThe)]
    public void A19_Xet_lai_van_khong_du_thi_khong_ve_CHO_XET(WorkflowFact fact)
    {
        var entity = new SVTrongDot { Id = 58, TrangThai = TrangThaiSVTrongDot.KHONG_DU_DK };
        var context = new WorkflowContext(WorkflowActorType.HeThong, null, new HashSet<string>(), null, null,
            DateTime.UtcNow, new HashSet<WorkflowFact> { fact }, []);

        var error = Assert.Throws<DomainException>(() => DATNWorkflow.Transition(entity, "T01R", context));

        Assert.Equal("UPM_WORKFLOW_GUARD_FAILED", error.Code);
        Assert.Equal(TrangThaiSVTrongDot.KHONG_DU_DK, entity.TrangThai);
    }

    [Theory]
    [InlineData(TrangThaiSVTrongDot.PHAI_LAM_LAI, 1)]
    [InlineData(TrangThaiSVTrongDot.KHONG_DU_DK, 2)]
    public void Tao_ho_so_lam_lai_lien_ket_ho_so_truoc(TrangThaiSVTrongDot status, int nextCount)
    {
        var previous = new SVTrongDot
        {
            Id = 56,
            TrangThai = status,
            SinhVienId = 7,
            CTDTId = 8,
            CoSoId = 9,
            LanLamLai = nextCount - 1,
        };

        var next = DATNWorkflow.TaoHoSoLamLai(previous, dotMoiId: 20);

        Assert.Equal(TrangThaiSVTrongDot.CHO_XET, next.TrangThai);
        Assert.Equal(56, next.HoSoTruocId);
        Assert.Equal(nextCount, next.LanLamLai);
        Assert.Equal(20, next.DotId);
        Assert.Equal(7, next.SinhVienId);
        Assert.Equal(8, next.CTDTId);
        Assert.Equal(9, next.CoSoId);
    }

    [Fact]
    public void Chuyen_theo_trang_thai_dich_hoat_dong_khi_chi_co_mot_ma_phu_hop()
    {
        var definition = Definition("T04");
        var entity = new SVTrongDot { TrangThai = definition.Tu };

        var result = DATNWorkflow.Transition(entity, definition.Den, Context(definition, useSystem: false));

        Assert.Equal("T04", result.Chuyen.MaChuyen);
    }

    private static WorkflowTransitionDefinition Definition(string code) => DATNWorkflow.DinhNghia.Single(x => x.MaChuyen == code);

    private static WorkflowContext Context(WorkflowTransitionDefinition definition, bool useSystem, IReadOnlySet<string>? roles = null)
    {
        var selected = definition.GuardGroups.Count > 0 ? definition.GuardGroups[0] : null;
        var facts = new HashSet<WorkflowFact>();
        var rules = new List<IKetQuaLuat>();
        if (selected is not null)
        {
            foreach (var condition in selected.Conditions)
            {
                switch (condition.Type)
                {
                    case WorkflowConditionType.Fact:
                        facts.Add(Enum.Parse<WorkflowFact>(condition.Ma));
                        break;
                    case WorkflowConditionType.RuleDat:
                        rules.Add(KetQuaLuat.Qua(condition.Ma, "Đạt."));
                        break;
                    case WorkflowConditionType.RuleKhongDat:
                        rules.Add(KetQuaLuat.KhongQua(condition.Ma, "Không đạt."));
                        break;
                    case WorkflowConditionType.CoItNhatMotRuleKhongDat:
                        rules.Add(KetQuaLuat.KhongQua(condition.CacMa![0], "Không đạt."));
                        break;
                }
            }
        }

        var roleSet = roles ?? (useSystem
            ? new HashSet<string>()
            : new HashSet<string> { definition.VaiTroDuocPhep.FirstOrDefault()! });
        var reason = definition.Den is TrangThaiSVTrongDot.PHAI_LAM_LAI or TrangThaiSVTrongDot.RUT ? "Lý do kiểm thử." : null;
        return new WorkflowContext(useSystem ? WorkflowActorType.HeThong : WorkflowActorType.NguoiDung,
            useSystem ? null : 1, roleSet, reason, null, DateTime.UtcNow, facts, rules);
    }
}

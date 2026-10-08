namespace UniProject.Share.Infrastructure.Constants
{
    public static class AppRoles
    {
        public const string Admin = "ADMIN";
        public const string GiangVien = "GIANG_VIEN";
        public const string SinhVien = "SINH_VIEN";

        public static readonly IReadOnlyList<string> All = new[] { Admin, GiangVien, SinhVien };
    }

}

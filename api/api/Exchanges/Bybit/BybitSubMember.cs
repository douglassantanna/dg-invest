namespace api.Exchanges.Bybit;

public class BybitSubMember
{
    public string Uid { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int MemberType { get; set; }
    public int Status { get; set; }
    public string Remark { get; set; } = string.Empty;
}

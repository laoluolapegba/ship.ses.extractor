namespace Ship.Ses.Extractor.Application.Authentication.ReLoginCustomer
{
    public sealed record RefreshUserTokenDto(string AccessToken, string RefreshToken);
}

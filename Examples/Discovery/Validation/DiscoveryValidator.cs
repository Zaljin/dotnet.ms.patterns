using FluentValidation;

namespace Discovery.Validation
{
    public class GetDiscoveryValidator : AbstractValidator<GetDiscoveryInputModel>
    {
        public GetDiscoveryValidator()
        {
            RuleFor(m => m.Name)
                .NotEmpty();

            RuleFor(m => m.Version)
                .NotEmpty()
                .Matches(@"^\d\.\d$");
        }
    }

    public class UpdateDiscoveryValidator : AbstractValidator<UpdateDiscoveryInputModel>
    {
        public UpdateDiscoveryValidator()
        {
            RuleFor(m => m.Name)
                .NotEmpty();

            RuleFor(m => m.Version)
                .NotEmpty()
                .Matches(@"^\d\.\d$");

            RuleFor(m => m.Endpoint)
                .Must(endpoint => Uri.IsWellFormedUriString(endpoint, UriKind.Absolute));
        }
    }
}

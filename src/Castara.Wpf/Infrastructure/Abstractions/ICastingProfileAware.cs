using Castara.Wpf.Models;

namespace Castara.Wpf.Infrastructure.Abstractions;


public interface ICastingProfileAware
{
    void SetCastingProfileOption(CastingProfileOption castingProfileOption);
}

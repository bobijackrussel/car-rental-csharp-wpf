using System;
using System.Globalization;

namespace CarRentalManagment.Services
{
    public interface ILocalizationService
    {
        CultureInfo CurrentCulture { get; }
        event EventHandler<CultureInfo>? LanguageChanged;
        void ApplyLanguage(string cultureName);
    }
}

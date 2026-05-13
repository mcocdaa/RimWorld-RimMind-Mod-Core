using RimMind.Application.Common.Interfaces.Abstractions;
using Verse;

namespace RimMind.Infrastructure.Verse
{
    public sealed class VerseTranslationService : ITranslationService
    {
        public string Translate(string key) => key.Translate();
        public string Translate(string key, object arg0) => key.Translate(arg0);
        public string Translate(string key, object arg0, object arg1) => key.Translate(arg0, arg1);
        public string Translate(string key, object arg0, object arg1, object arg2) => key.Translate(arg0, arg1, arg2);
    }
}

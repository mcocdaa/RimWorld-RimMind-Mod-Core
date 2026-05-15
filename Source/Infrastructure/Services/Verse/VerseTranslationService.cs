using RimMind.Application.Common.Interfaces.Abstractions;
using Verse;

#pragma warning disable CS0618

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

#pragma warning restore CS0618

using System;

namespace KantarPro.Desktop
{
    public sealed class SearchDateCriteria
    {
        private SearchDateCriteria(DateTime? startInclusive, DateTime? endExclusive)
        {
            StartInclusive = startInclusive;
            EndExclusive = endExclusive;
        }

        public DateTime? StartInclusive { get; private set; }

        public DateTime? EndExclusive { get; private set; }

        public static SearchDateCriteria Create(DateTime? start, DateTime? end, string label)
        {
            var startInclusive = start.HasValue ? (DateTime?)start.Value.Date : null;
            var endExclusive = end.HasValue ? (DateTime?)end.Value.Date.AddDays(1) : null;

            if (startInclusive.HasValue &&
                endExclusive.HasValue &&
                endExclusive.Value <= startInclusive.Value)
            {
                throw new InvalidOperationException(
                    label + " bitiş tarihi başlangıç tarihinden önce olamaz.");
            }

            return new SearchDateCriteria(startInclusive, endExclusive);
        }
    }
}

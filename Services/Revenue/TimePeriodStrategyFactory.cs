namespace GiaLaiOCOP.Api.Services.Revenue
{
    /// <summary>
    /// Factory để tạo TimePeriodStrategy phù hợp
    /// </summary>
    public class TimePeriodStrategyFactory
    {
        private readonly Dictionary<string, ITimePeriodStrategy> _strategies;

        public TimePeriodStrategyFactory()
        {
            _strategies = new Dictionary<string, ITimePeriodStrategy>(StringComparer.OrdinalIgnoreCase)
            {
                { "week", new WeekPeriodStrategy() },
                { "month", new MonthPeriodStrategy() },
                { "year", new YearPeriodStrategy() }
            };
        }

        /// <summary>
        /// Lấy strategy theo type
        /// </summary>
        public ITimePeriodStrategy GetStrategy(string type)
        {
            if (!_strategies.TryGetValue(type, out var strategy))
            {
                throw new ArgumentException($"Loại thời gian '{type}' không được hỗ trợ. Chỉ hỗ trợ: week, month, year");
            }

            return strategy;
        }

        /// <summary>
        /// Kiểm tra type có được hỗ trợ không
        /// </summary>
        public bool IsSupported(string type)
        {
            return _strategies.ContainsKey(type);
        }
    }
}


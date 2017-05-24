/*
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Configuration;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace Optimization.Example
{
    public class ParameterizedAlgorithm : QCAlgorithm
    {
        private readonly decimal stopLoss = Config.GetValue("stop", 0.2m);
        private readonly decimal takeProfit = Config.GetValue("take", 0.1m);
        public ExponentialMovingAverage Fast;
        public int FastPeriod = Config.GetInt("fast", 13);
        public ExponentialMovingAverage Slow;

        public int SlowPeriod = Config.GetInt("slow", 56);

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);
            SetEndDate(2013, 10, 11);
            SetCash(100 * 1000);

            AddSecurity(SecurityType.Equity, "SPY");

            Fast = EMA("SPY", FastPeriod);
            Slow = EMA("SPY", SlowPeriod);
        }

        public void OnData(TradeBars data)
        {
            // wait for our indicators to ready
            if (!Fast.IsReady || !Slow.IsReady) return;

            if (!Portfolio["SPY"].HoldStock)
            {
                if (Fast > Slow * 1.001m)
                {
                    SetHoldings("SPY", 1);
                }
            }
            else if (Portfolio["SPY"].UnrealizedProfitPercent > takeProfit ||
                     Portfolio["SPY"].UnrealizedProfitPercent < stopLoss)
            {
                Liquidate("SPY");
            }
        }
    }
}
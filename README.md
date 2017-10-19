# Minimizing the Probability of Ruin in Retirement

## C# application how to use

### How to run

Run 

    # MinimizeRuinProbability.exe [<optional parameter: number of threads>]
    
Input files are created automatically with default values. 
A single parameter is optionally accepted which is the number of tasks the user wants to process concurrently as it runs. If no value is supplied the code determines the maximum number of concurrent processing units on the machine running it and uses this value. The maximum number of concurrent processing units is generally the number of CPU cores.

### Configure

There are two input files that are created automatically in "./config/" directory if do not exist.

#### control.txt

Format:

<pre>
StockMean StockVar BondMean BondVar StockBondCov RF<sub>Max</sub> E<sub>R</sub> PrunePwr
P<sub>R</sub> P<sub>α</sub>
NumRand Details
</pre>


Where:

***StockMean, StockVar, BondMean, BondVar, StockBondCov*** - the means/variances match the historical values. Probably there is a sense to update these values once per several years. But they are not changing too much with time, so you can leave it constant always.

***RF<sub>Max</sub>*** – max Ruin Factor(RF) that is used in calculations and results. It’s set to 2.75, but for RF=1 and more results are almost useless because of ruin probability is almost 1, because of it means your investment returns should be doubled within the year to cover your needs.

***E<sub>R</sub>*** - expense ratio charged by the financial institution per time t. 0.005 value, means 0.5% are charged.

***PrunePwr*** – is used to improve the performance for multi person (MPU) calculations. 

***P<sub>R</sub>*** - discretization precision for RF calculation, i.e. how many buckets (discrete values per 1 RF value) to use to calculate numerical approximation to the optimal decumulation strategy (See 3.7.4 section from the original doc).

***P<sub>α</sub>*** - discretization precision for *α*.

***NumRand*** – use 0 for fixed number of years in retirement, or N for 1+ persons for random retires calculations (See 4.6.2 from original doc for details), with age, based on ageprobs.txt.

***Details*** – if *NumRand* is 0: \<T<sub>D</sub> - number of years in retirement\>. If *NumRand* > 0: pairs of two values: \<gender letter M or F\> \<person age\>.

#### ageprobs.txt

Columns in this file represent: age, male death probability, and female death probability for a 50-year old. They are computed from life-tables published at SSA.gov and each column sums to 1.

### Output

4 files as result:
* *MinimizeRuinProbability.exe.log* - log file, all you see in console saved there.

* *FinalAlphaResults_H.csv* - α values for a given RF value and time - optimal asset allocation in stocks.

* *FinalProbResults_H.csv* - V<sub>R</sub> values for a given RF value and time – minimum probability of ruin table.

* *FinalResults_V.txt* - concatenation of two previous files. Each row is time point [t], ruin factor RF(t), minimum probability of ruin V<sub>R</sub>(t, RF(t)), and optimal asset allocation α<sub>R</sub>(t, RF(t)).

### How to use

The app gives the answer to the question – what’s the optimal amount should be invested to stocks (and the rest to bonds) and what’s the probability of ruin in this case, with the given value of retirement account and the given initial withdrawal rate adjusted for inflation each year. This optimal portfolio balance (and ruin probability) recalculated each year, based on the level of inflation and portfolio returns.

#### Example


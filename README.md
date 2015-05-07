# About
LivePercentiles is an experiment aimed at comparing different ways of computing percentiles on the fly. The current version implements two basic *a posteriori* methods (that store the data) and two *live* methods (that compute the percentiles as they go).

# What's in it?

## A posteriori methods
Those methods store all the provided data and compute the percentiles at the end, they are only meant for comparison and are very inefficient in their memory usage.

#### Nearest rank
This method is taken from [Wikipedia's percentiles page](http://en.wikipedia.org/wiki/Percentile#The_Nearest_Rank_method) and is used as a reference for comparisons.

#### Linear interpolation between closest ranks
This method is also taken from [Wikipedia's basic methods](http://en.wikipedia.org/wiki/Percentile#The_Linear_Interpolation_Between_Closest_Ranks_method) and is used as another reference implementation


## Live methods
#### P² single value algorithm
This calculation method is taken from [[`The P² algorithm for dynamic calculation of quantile and histograms without storing observations. by R. Jain and I. Chlamtac.`]](http://www.cse.wustl.edu/~jain/papers/ftp/psqr.pdf). It estimates the value of a given unique percentile using very little memory (<300 bytes).

#### P² histogram algorithm
This calculation method is also taken from Jain and Chlamtac paper, it estimates a range of percentiles given a number of buckets (<300 bytes too).

# Upcoming
* Comparison between the different methods using different data distributions (other than unit tests)
* Comparison with existing libraries (HdrHistogram, etc.)
* More live methods


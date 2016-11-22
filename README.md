# RecSys
Location recommendation GUI combining user-based collaborative filtering, power law distribution and Multi-gaussian model

This GUI is created based on Sina weibo's user checkin data, it could be changed.

Different recommendation strategies could be used seperately,

such as user-based collaborative filtering, power law distribution and multi-gaussian model,

and also, they could be combined together linearly and non-linearly, results show explicitly.

It's created for my dissertation, if any questions and suggestions, please leave a comment or e-mail ryogaw1994@gmail.com. 

I'm available and happy to discuss:)


-----------------------------------------------------------------------------------------------------
Initialization:

We test effective of recommendation by cross validation set. First, data would be randomly splitted as training set and test set for different ratios(10%, 20%, 30% in test set).

1) DataSplitter: including functions to load data, randomly split dataset by specified ratio, output data into csv file, retrieve existing dataset, measure recall and precision;

2) rectangle: object indicating a rectangle including top left and down right angle. Function .contains checks whether given location is contained in this rectangle;


In this program, there're several combinations of recommendation strategies including:

1. PL.cs: implement recommendation by power law distribution
2. MGM.cs: implement recommendation by multi-gaussian distribution
3. L_MGM_UCF.cs:linear combination of multi-gaussian distribution and user-based collaborative filtering
4. L_PL_UCF.cs: linear combination of power law distribution and user-based collaborative filtering
5. MGM_UCF.cs: combination of multi-gaussian distribution and user-based collaborative filtering(faster)
6. PL_UCF.CS: combination of power law distribution and user-based collaborative filtering(faster)
7. User_CF.cs: user-based collaborative filtering

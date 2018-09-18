import pandas as pd
import matplotlib.pyplot as plt

fileName = r'debug.csv'
dataFrame = pd.read_csv(fileName,header=None)
dataFrame.plot()
plt.ylim(0,25)
plt.show()
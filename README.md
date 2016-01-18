LifeGameManager
===============
Automatic evaluator tool for the homework project called [*LifeGame*](http://mit.bme.hu/~engedy/LifeGame2015.pdf) for the class [Cooperative and learning systems](http://www.mit.bme.hu/oktatas/targyak/vimia357) at BME.

The tool automatically checks for new homework submissions over a mysql connection. 
If a new submission is found, the evaluator script for the submitted homework is launched in a new instance of Matlab. 
After the evaluation has finished, the results are written back to the database over the mysql connection.

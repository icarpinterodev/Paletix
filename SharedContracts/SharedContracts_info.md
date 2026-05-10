# SharedContracts
El projecte SharedContracts és una biblioteca de classes que conté les definicions de les interfícies
i contractes que seran utilitzats per altres projectes dins del mateix solució. 
Aquest projecte no conté implementacions concretes, sinó que serveix com a punt de referència per a les altres parts del sistema.

Per exemple, si tenim un projecte de servei que necessita exposar una API,
les interfícies que defineixen els contractes d'aquesta API es trobaran en el projecte SharedContracts. 
Això permet que altres projectes, com ara clients o altres serveis, puguin referenciar aquestes interfícies i 
implementar-les segons sigui necessari.

En aquest cas Shared contracts s'utilitza per la aplicació WinUI3 (PaletixDesktop) i la API REST (MagatzapiV2)
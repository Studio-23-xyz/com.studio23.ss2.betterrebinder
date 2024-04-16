<h1 align="center">Better Rebinder</h1>
<p align="center">
<a href="https://openupm.com/packages/com.studio23.ss2.betterebinder/"><img src="https://img.shields.io/npm/v/com.studio23.ss2.betterebinder?label=openupm&amp;registry_uri=https://package.openupm.com" /></a>

</p>


Better Rebinder provides an easy and intuitive way to rebind your inputs with the new Input System for Unity. 


## Table of Contents

1. [Installation](#installation)
	- [Git URL](#git-url)
	- [Package Manager](#package-manager)
2. [Getting Started](#getting-started)
   - [Setup](#setup)
3. [Saving and Loading Overrides](#saving-and-loading-data)
   - [Saving Input Overrides](#saving-input-overrides)
   - [Load Input Overrides](#load-input-overrides)


## Installation


###  Git URL
You can also use the "Install from Git URL" option from Unity Package Manager to install the package.
```
https://github.com/Studio-23-xyz/com.studio23.ss2.betterrebinder.git#upm
```

### Package Manager

1. open Edit/Project Settings/Package Manager
2. add a new Scoped Registry (or edit the existing OpenUPM entry)
```
Name 	package.openupm.com
URL 	 https://package.openupm.com
Scope(s) com.cysharp.unitask 
         com.studio23.ss2.betterrebinder
		 com.studio23.ss2.buttoniconresourcemanager
```
3. click Save or Apply
4. open Window/Package Manager
5. click +
6. select Add package by name... or Add package from git URL...
7. paste com.studio23.ss2.betterrebinder into name
8. click Add


## Getting Started

### Initialize

Yes. 

## Saving and Loading Data

### Saving Input Overrides

`Note: You must implement IsDirty property for ISavables`

### Load Input Overrides

Loading data is as simple as calling the LoadMethod.

## Other Links

[OpenUPM Page](https://openupm.com/packages/com.studio23.ss2.savesystem/)

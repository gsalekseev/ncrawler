ncrawler
========

Copy of NCrawler from http://ncrawler.codeplex.com/

Simple and very efficient multithreaded web crawler with pipeline based processing written in C#. 
Contains HTML, Text, PDF, and IFilter document processors and language detection(Google). 
Easy to add pipeline steps to extract, use and alter information.

## Build Nuget packages

Create debug packages

    .\Build.ps1 -VersionSuffix build002

Create release packages

    .\Build.ps1

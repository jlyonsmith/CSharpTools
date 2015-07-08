# Source Code Tools

A collection of useful command line development tools for source code maintenance. They are written in [C#](http://en.wikipedia.org/wiki/C_Sharp_&lpar;programming_language&rpar;) and use the [Mono](http://www.mono-project.com/) platform.  I use [Xamarin Studio](http://xamarin.com/studio) as the IDE for maintaining the tools.

- __Strapper__ generates strongly type C# wrappers for string resources.  Useful in conjunction with my [ToolBelt](https://github.com/jlyonsmith/ToolBelt) project for generating strongly typed string resource wrappers.
- __Ender__ reports on and fixing line endings
- __Vamper__ updates file and product version numbers across a variety of different file types
- __Doozer__ displays //TODO: comments in C# source files
- __Lindex__ generates a line index for text base log files
- __Projector__ clones a project and renames and re-guids the project files
- __Popper__ swaps a project back and forth between a NuGet and local project references

## Installation

The latest version of the tools can be installed using [HomeBrew](http://brew.sh) with:

    brew install https://gist.githubusercontent.com/jlyonsmith/288321e7dec8520761c2/raw/45b0b2a9c21fa088c8d6f375b37f11cf28bd923c/codetools.rb

[![Build Status](https://travis-ci.org/jlyonsmith/CodeTools.svg?branch=master)](https://travis-ci.org/jlyonsmith/CodeTools)

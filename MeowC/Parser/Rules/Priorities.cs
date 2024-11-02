﻿namespace MeowC.Parser.Rules;

public enum Priorities
{
	No,
	Assignment,
	FunctionFormation,
	Conditional,
	Sum,
	Product,
	Exponent,
	Prefix,
	Postfix,
	Const
}
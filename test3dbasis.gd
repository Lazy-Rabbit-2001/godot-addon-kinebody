@tool
extends Node3D

@export var result: int:
	set(_value):
		var up := Vector3.UP
		print(Quaternion(up, Vector3.UP))

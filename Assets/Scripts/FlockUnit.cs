using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlockUnit : MonoBehaviour
{
	public Animator animator;
	[SerializeField] private float _FOVAngle;
	public float FOVAngle { get { return _FOVAngle; } }

	[SerializeField] private float _smoothDamp;
	public float smoothDamp { get { return _smoothDamp; } }

	[SerializeField] private LayerMask obstacleMask;

	private Vector3 _currentVelocity;
	public Vector3 currentVelocity { get { return _currentVelocity; } set { _currentVelocity = value; } }

	private Flock assignedFlock;

	public float speed;

	public Transform myTransform { get; set; }

	private void Awake()
	{
		myTransform = transform;
		animator = GetComponent<Animator>();
	}

	public void AssignFlock(Flock flock)
	{
		assignedFlock = flock;
	}

	public void InitializeSpeed(float speed)
	{
		this.speed = speed;
	}


}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// Debug.Log() prints to Console while Grapher.Log() outputs to Grapher. Grapher can be opened under "Window" dropdown at top in Unity

public class RunScript : MonoBehaviour
{
	// initialize some of our variables
	// go down to "Start" for more initialization of variables
	private float kb = 0.0019872036f;
	public float T = 100;
	private float kbt;
	public GameObject Circle, Box;
	public float L, sigma;
	public int N;
	private float ro, E, m;
	public int totsteps = 0;
	public float waittime = 0.000000000001f;
	float x, y, newx, newy;
	List<List<Vector2>> accepted = new List<List<Vector2>>();
	public float epsilon, R;
	public float stepsize;
	public Toggle one, five;
	public Slider SigmaS, RS, EpsilonS, StepSizeS;
	public Toggle HST, SWT, LJT;
	bool hsb, swb, ljb;
	public Slider WaitTimeS;
	public Slider LSlider;

	// a bunch of functions here for the buttons and sliders. 

	void Clear()
	{
		hsenergy = 0; ljenergy = 0; swenergy = 0; oldtryhsenergy = 0; oldtryswenergy = 0; oldtryljenergy = 0; tryljenergy = 0; tryhsenergy = 0; tryswenergy = 0;
		HSdEL.Clear(); SWdEL.Clear(); LJdEL.Clear();
		totsteps = 0;
		accepted.Clear();
	}

	public void HSToggle()
	{
		Clear();
		if (HST.isOn)
		{
			hsb = true;
			swb = false;
			ljb = false;
			SWT.isOn = false;
			LJT.isOn = false;
		}
	}

	public void SWToggle()
	{
		Clear();
		if (SWT.isOn)
		{
			hsb = false;
			swb = true;
			ljb = false;
			HST.isOn = false;
			LJT.isOn = false;
		}
	}

	public void LJToggle()
	{
		StopAllCoroutines();
		Clear();

		if (LJT.isOn)
		{
			ljb = true;
			swb = false;
			hsb = false;
			SWT.isOn = false;
			HST.isOn = false;
		}
		StartCoroutine(Mover());
	}

	public void ToggleOne()
	{
		StopAllCoroutines();
		Clear();
		if (one.isOn)
		{
			five.isOn = false;
			T = 100;
		}
		StartCoroutine(Mover());
	}

	public void ToggleFive()
	{
		StopAllCoroutines();
		Clear();
		if (five.isOn)
		{
			one.isOn = false;
			T = 500;
		}
		StartCoroutine(Mover());
	}

	public void SigmaSlider()
	{
		StopAllCoroutines();
		sigma = SigmaS.value;
		Clear();
		foreach (GameObject c in GameObject.FindGameObjectsWithTag("circles"))
		{
			c.transform.localScale = new Vector2(SigmaS.value, SigmaS.value);
		}
		StartCoroutine(Mover());
	}

	public void EpsilonSlider()
	{
		StopAllCoroutines();
		epsilon = EpsilonS.value;
		Clear();
		StartCoroutine(Mover());
	}

	public void RSlider()
	{
		StopAllCoroutines();
		Clear();
		StartCoroutine(Mover());
	}

	public void StepSizeSlider()
	{
		StopAllCoroutines();
		Clear();
		stepsize = StepSizeS.value;
		StartCoroutine(Mover());
	}

	public void WaitTimeSlider()
	{
		StopAllCoroutines();
		Clear();
		waittime = WaitTimeS.value;
		StartCoroutine(Mover());
	}

	public void LSliderButton()
	{
		StopAllCoroutines();
		Clear();
		L = LSlider.value;
		Box.transform.localScale = new Vector3(L, L, 1);
		StartCoroutine(Mover());
	}

	void Start()
	{
		hsb = true;
		stepsize = 0.1f;
		int i = 0, j = 0;
		N = 150;
		L = 5;
		sigma = 0.15f;
		epsilon = 1f;
		R = 2f;
		float padding = 1.2f;
		T = 100;
		Box.transform.localScale = new Vector3(L, L, 1);
		ro = N / (float)Math.Pow(L, 2);
		Debug.Log(String.Format("Density = {0} particles per angstrom squared.", ro));
		Debug.Log(String.Format("Box size = {0} Angstrom", L));

		// Instantiate the circles
		for (i = 0; i < N; i++)
		{
			Instantiate(Circle);
			Circle.transform.tag = "circles";
		}
		i = 0;
		int k = 0;
		List<Vector2> seed = new List<Vector2>();

		// Move them in a new loop because you aren't allowed to in the loop above (Unity does this to prevent data corruption)
		foreach (GameObject c in GameObject.FindGameObjectsWithTag("circles"))
		{
			c.transform.localPosition = new Vector3(-L / 2 + k * sigma * padding, L / 2 - j * sigma * padding, 0);

			c.transform.localScale = new Vector3(sigma, sigma, 1);
			c.transform.name = String.Format("Circle {0}", i);

			k++;
			i++;
			if (-L / 2 + k * sigma * padding >= L / 2)
			{
				k = 0;
				j++;

			}
			seed.Add(c.transform.localPosition);
		}

		// we check these bools based on which energy function is toggled
		if (hsb)
		{
			hsenergy = HardSphereEnergySeed(seed);
		}
		else if (swb)
		{
			swenergy = SquareWellEnergySeed(seed);
		}
		else if (ljb)
		{
			ljenergy = LennardJonesEnergySeed(seed);
		}

		// we start the coroutine Mover(), this gives us more control than an Update() function which updates on every frame. 
		// you can tell a coroutine to wait for a certain amount of time, or wait until some condition is true
		// you can also use asynchronous functions in C# for waiting until a condition is met before continuing
		coroutonoff = true;
		StartCoroutine(Mover());
	}

	// These are just for computing the energy for our seed
	float HardSphereEnergySeed(List<Vector2> coords)
	{
		float thisenergy = 0;
		float totenergy = 0;
		foreach (Vector2 circone in coords)
		{
			foreach (Vector2 circtwo in coords)
			{
				if (circone != circtwo && coords.IndexOf(circtwo) > coords.IndexOf(circone))
				{
					if (Vector2.Distance(circone, circtwo) >= sigma)
					{
						thisenergy = 0;
					}
					else
						thisenergy = 1f / 0f;
					totenergy += thisenergy;
				}
			}
		}
		return totenergy;
	}

	float SquareWellEnergySeed(List<Vector2> coords)
	{
		float thisenergy = 0;
		float totenergy = 0;

		foreach (Vector2 circone in coords)
		{
			foreach (Vector2 circtwo in coords)
			{
				if (circone != circtwo && coords.IndexOf(circtwo) > coords.IndexOf(circone))
				{
					if (Vector2.Distance(circone, circtwo) < sigma)
					{
						thisenergy = 1f / 0f;
					}
					else if (Vector2.Distance(circone, circtwo) >= sigma && Vector2.Distance(circone, circtwo) <= R * sigma)
					{
						thisenergy = -1 * epsilon;
					}
					else if (Vector2.Distance(circone, circtwo) > R * sigma)
						thisenergy = 0;
					totenergy += thisenergy;
				}
			}
		}
		return totenergy;
	}

	float LennardJonesEnergySeed(List<Vector2> coords)
	{
		float thisenergy = 0;
		float totenergy = 0;
		foreach (Vector2 circone in coords)
		{
			foreach (Vector2 circtwo in coords)
			{
				if (circone != circtwo && coords.IndexOf(circtwo) > coords.IndexOf(circone))
				{
					float r = Vector2.Distance(circone, circtwo);
					thisenergy = 4 * epsilon * (float)(Math.Pow(sigma / r, 12) - Math.Pow(sigma / r, 6));
				}
				totenergy += thisenergy;
			}
		}
		return totenergy;
	}

	public bool coroutonoff;
	IEnumerator Mover()
	{
		while (true)
		{
			MoveIt();
			yield return new WaitForSeconds(waittime);
			// waittime can be set to 0 if you would like to not slow down at all
		}
	}

	float hsenergy, oldtryhsenergy, tryhsenergy;
	float swenergy, oldtryswenergy, tryswenergy;
	float ljenergy, oldtryljenergy, tryljenergy;
	float HSdE, SWdE, LJdE;
	List<float> HSdEL = new List<float>();
	List<float> SWdEL = new List<float>();
	List<float> LJdEL = new List<float>();

	void MoveIt()
	{
		List<Vector2> thistry = new List<Vector2>();
		List<Vector2> oldtry = new List<Vector2>();
		int randno = UnityEngine.Random.Range(0, N);
		GameObject e = GameObject.FindGameObjectsWithTag("circles")[randno];

		Vector2 changedatom = new Vector2();
		Vector2 oldenergy = new Vector2();

		foreach (GameObject d in GameObject.FindGameObjectsWithTag("circles"))
		{
			Vector2 thisone = new Vector2(d.transform.localPosition.x, d.transform.localPosition.y);
			oldtry.Add(d.transform.localPosition);
			if (Array.IndexOf(GameObject.FindGameObjectsWithTag("circles"), d) == randno)
			{
				x = e.transform.localPosition.x;
				y = e.transform.localPosition.y;

				oldenergy = new Vector2(x, y);
				float xr = UnityEngine.Random.Range(-stepsize, stepsize);
				float yr = UnityEngine.Random.Range(-stepsize, stepsize);

				newx = x + xr;

				if (newx > L / 2 || newx < -L / 2)
					newx = -x + xr;
				else
					newx = x + xr;

				newy = y + yr;

				if (newy > L / 2 || newy < -L / 2)
					newy = -y + yr;
				else
					newy = y + yr;

				thisone = new Vector2(newx, newy);
				changedatom = thisone;
			}
			thistry.Add(thisone);
		}

		// we start with this bool set to true. if our energy goes up and our P() < alpha then this will be changed to false
		bool validmove = true;
		float alpha = UnityEngine.Random.Range(0f, 1f);

		if (hsb)
		{
			oldtryhsenergy = HardSphereEnergy(oldtry, oldenergy);
			tryhsenergy = HardSphereEnergy(thistry, changedatom);
			if (hsenergy - oldtryhsenergy + tryhsenergy <= hsenergy)
				goto Accepted;
			HSdE = -oldtryhsenergy + tryhsenergy;
			if (P(HSdE) < alpha)
				validmove = false;
		}
		else if (swb)
		{
			oldtryswenergy = SquareWellEnergy(oldtry, oldenergy);
			tryswenergy = SquareWellEnergy(thistry, changedatom);
			if (swenergy - oldtryswenergy + tryswenergy <= swenergy)
				goto Accepted;
			SWdE = -oldtryswenergy + tryswenergy;
			if (P(SWdE) < alpha)
				validmove = false;
		}
		else if (ljb)
		{
			oldtryljenergy = LennardJonesEnergy(oldtry, oldenergy);
			tryljenergy = LennardJonesEnergy(thistry, changedatom);
			if (ljenergy - oldtryljenergy + tryljenergy <= ljenergy)
				goto Accepted;
			LJdE = -oldtryljenergy + tryljenergy;
			if (P(LJdE) < alpha)
				validmove = false;
		}

		Accepted:
		if (validmove == true)
		{
			if (hsb)
			{
				hsenergy += -oldtryhsenergy + tryhsenergy;
				// Grapher doesn't show 0 values so this will not show up unless a small fudge amount is added to hsenergy in this function
				//Grapher.Log(hsenergy + 0.000000001f, "HS Energies");
				Grapher.Log(hsenergy, "HS Energies"); 
				HSdEL.Add(hsenergy);
				Grapher.Log(HSdEL.Average(), "Avg HS Energy");
			}
			else if (swb)
			{
				swenergy += -oldtryswenergy + tryswenergy;
				Grapher.Log(swenergy, "SW Energies");
				SWdEL.Add(swenergy);
				Grapher.Log(SWdEL.Average(), "Avg SW Energy");
			}
			else if (ljb)
			{
				ljenergy += -oldtryljenergy + tryljenergy;
				Grapher.Log(ljenergy, "LJ Energies");
				LJdEL.Add(ljenergy);
				Grapher.Log(LJdEL.Average(), "Avg LJ Energy");
			}

			accepted.Add(thistry);
			e.transform.localPosition = changedatom;

			// do distance calculation every 20 accepted steps because it slows the program and we don't really care about each individual step, just about convergence
			if (dist && accepted.Count % 20 == 0)
				Debug.Log(String.Format("Average distance: {0} Angstroms.", AvgDis()));
		}

		totsteps++;

		float value = 100f * accepted.Count / (float)totsteps;

		Grapher.Log(value, "Percent accepted");
	}

	public bool dist = false;

	public void CalcDis()
	{
		if (dist)
		{
			dist = false;
			return;
		}
		else
		{
			dist = true;
		}
	}


	float AvgDis()
	{
		GameObject[] coords = GameObject.FindGameObjectsWithTag("circles");
		float dis = 0;

		foreach (GameObject one in coords)
		{
			foreach (GameObject two in coords)
			{
				if (one != two && Array.IndexOf(coords, two) > Array.IndexOf(coords, one))
					dis += Vector2.Distance(one.transform.localPosition, two.transform.localPosition);
			}
		}

		return dis / (N * (N - 1) / 2);
	}

	// this is done just so the logic is consistent for all three V(x)'s 
	float HardSphereEnergy(List<Vector2> coords, Vector2 thistwo)
	{
		float thisenergy = 0;
		float totenergy = 0;
		foreach (Vector2 circone in coords)
		{
			if (circone != thistwo)
			{
				if (Vector2.Distance(circone, thistwo) >= sigma)
					thisenergy = 0;
				else
					thisenergy = 1f / 0f; // this is done to make it infinity
				totenergy += thisenergy;
			}
		}
		return totenergy;
	}

	float SquareWellEnergy(List<Vector2> coords, Vector2 thistwo)
	{
		float thisenergy = 0;
		float totenergy = 0;

		foreach (Vector2 circone in coords)
		{
			if (circone != thistwo)
			{
				if (Vector2.Distance(circone, thistwo) < sigma)
					thisenergy = 1f / 0f;
				else if (Vector2.Distance(circone, thistwo) >= sigma && Vector2.Distance(circone, thistwo) <= R * sigma)
					thisenergy = -1 * epsilon;
				else if (Vector2.Distance(circone, thistwo) > R * sigma)
					thisenergy = 0;
				totenergy += thisenergy;
			}
		}
		return totenergy;
	}

	float LennardJonesEnergy(List<Vector2> coords, Vector2 thistwo)
	{
		float thisenergy = 0;
		float totenergy = 0;
		foreach (Vector2 circone in coords)
		{
			if (circone != thistwo)
			{
				float r = Vector2.Distance(circone, thistwo);
				thisenergy = 4 * epsilon * (float)(Math.Pow(sigma / r, 12) - Math.Pow(sigma / r, 6));
				totenergy += thisenergy;
			}
		}
		return totenergy;
	}

	float P(float dE)
	{
		kbt = kb * T;
		return (float)Math.Exp(-1 * dE / kbt);
	}

	public void TurnOffOn()
	{
		if (coroutonoff)
		{
			coroutonoff = false;
			StopAllCoroutines();
			return;
		}
		else
		{
			coroutonoff = true;
			StartCoroutine(Mover());
		}
	}

	// I was having trouble getting this to work calculating for multiple frames
	public void g()
	{
		string txt = "";
		GameObject[] coords = GameObject.FindGameObjectsWithTag("circles");
		List<float> rs = new List<float>();
		List<float> grs = new List<float>();

		float dr = 0.01f;

		for (float r = 0.01f; r < L; r += 0.1f)
		{
			float gr = 0;

			foreach (GameObject one in coords)
			{
				// The circles all have colliders and rigidbodies on them so we can use Unity's physics to see if they overlap
				gr += (Physics2D.OverlapCircleAll(one.transform.localPosition, r + dr).Length);
				gr -= (Physics2D.OverlapCircleAll(one.transform.localPosition, r).Length);
			}

			gr /= N * 2 * (float)Math.PI * r * dr * ro;

			txt += String.Format("{0}\t{1}\n", r, gr);
		}

		// Change this path. This can be opened in Excel.
		var path = "C:/Users/Roitberg Lab/Documents/data/gr.txt";

		System.IO.File.WriteAllText(path, txt);

	}

	float Zfunc(List<Vector2> coordinates)
	{
		kbt = kb * T;
		float beta = 1 / kbt;
		float Z = 0;
		GameObject[] coords = GameObject.FindGameObjectsWithTag("circles");

		foreach (GameObject one in coords)
		{
			if (hsb)
			{
				Z += (float)Math.Exp(-beta * HardSphereEnergy(coordinates, one.transform.localPosition));
			}
			else if (swb)
			{
				Z += (float)Math.Exp(-beta * SquareWellEnergy(coordinates, one.transform.localPosition));
			}
			else if (ljb)
			{
				Z += (float)Math.Exp(-beta * LennardJonesEnergy(coordinates, one.transform.localPosition));
			}
		}
		return Z;
	}

	// I'm not sure how to calculate the dN or dV parts for mu and P
	void mu(List<Vector2> coordinates)
	{
		float Z = Zfunc(coordinates);

		// mu = dE / dN ) _S,V
	}

	void p(List<Vector2> coordinates)
	{
		float Z = Zfunc(coordinates);
		// p = - dE/dV ) _S,N
	}

	float s(List<Vector2> coordinates, float energy)
	{
		float Z = Zfunc(coordinates);
		kbt = kb * T;
		float beta = 1 / kbt;
		// S=kBlnZ+kBβU−kBβ〈N〉
		float S = kb * (float)Math.Log(Z) + (kb * beta * energy) - kb * beta * N;
		return S;
	}
}
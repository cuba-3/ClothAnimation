using UnityEngine;
using System.Collections;
using System;

public class cloth_motion: MonoBehaviour {

	float 		t;
	int[] 		edge_list;
	float 		mass;
	float		damping;
	float 		stiffness;
	float[] 	L0;
	Vector3[] 	velocities;
	float dt;
	//Vector3[] vertices;

	// Use this for initialization
	void Start () 
	{
		t 			= 0.075f;
		mass 		= 1.0f;
		damping 	= 0.99f;
		stiffness 	= 1000.0f;
		dt = 0.02f;

		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		int[] 		triangles = mesh.triangles;
		Vector3[] vertices = mesh.vertices;

		//Construct the original edge list
		int[] original_edge_list = new int[triangles.Length*2]; // every two integers represent an edge
		for (int i=0; i<triangles.Length; i+=3) 
		{
			original_edge_list[i*2+0]=triangles[i+0];
			original_edge_list[i*2+1]=triangles[i+1];
			original_edge_list[i*2+2]=triangles[i+1];
			original_edge_list[i*2+3]=triangles[i+2];
			original_edge_list[i*2+4]=triangles[i+2];
			original_edge_list[i*2+5]=triangles[i+0];
		}
		//Reorder the original edge list
		for (int i=0; i<original_edge_list.Length; i+=2)
			if(original_edge_list[i] > original_edge_list[i + 1]) 
				Swap(ref original_edge_list[i], ref original_edge_list[i+1]);
		//Sort the original edge list using quicksort
		Quick_Sort (ref original_edge_list, 0, original_edge_list.Length/2-1);

		int count = 0;
		for (int i=0; i<original_edge_list.Length; i+=2)
			if (i == 0 || 
			    original_edge_list [i + 0] != original_edge_list [i - 2] ||
			    original_edge_list [i + 1] != original_edge_list [i - 1]) 
					count++;

		edge_list = new int[count * 2];
		int r_count = 0;
		for (int i=0; i<original_edge_list.Length; i+=2)
			if (i == 0 || 
			    original_edge_list [i + 0] != original_edge_list [i - 2] ||
				original_edge_list [i + 1] != original_edge_list [i - 1]) 
			{
				edge_list[r_count*2+0]=original_edge_list [i + 0];
				edge_list[r_count*2+1]=original_edge_list [i + 1];
				r_count++;
			}


		L0 = new float[edge_list.Length/2];
		for (int e=0; e<edge_list.Length/2; e++) 
		{
			int v0 = edge_list[e*2+0];
			int v1 = edge_list[e*2+1];
			L0[e]=(vertices[v0]-vertices[v1]).magnitude;
		}

		velocities = new Vector3[vertices.Length];
		for (int v=0; v<vertices.Length; v++)
			velocities [v] = new Vector3 (0, 0, 0);

		//for(int i=0; i<edge_list.Length/2; i++)
		//	Debug.Log ("number"+i+" is" + edge_list [i*2] + "and"+ edge_list [i*2+1]);
	}

	void Quick_Sort(ref int[] a, int l, int r)
	{
		int j;
		if(l<r)
		{
			j=Quick_Sort_Partition(ref a, l, r);
			Quick_Sort (ref a, l, j-1);
			Quick_Sort (ref a, j+1, r);
		}
	}

	int  Quick_Sort_Partition(ref int[] a, int l, int r)
	{
		int pivot_0, pivot_1, i, j;
		pivot_0 = a [l * 2 + 0];
		pivot_1 = a [l * 2 + 1];
		i = l;
		j = r + 1;
		while (true) 
		{
			do ++i; while( i<=r && (a[i*2]<pivot_0 || a[i*2]==pivot_0 && a[i*2+1]<=pivot_1));
			do --j; while(  a[j*2]>pivot_0 || a[j*2]==pivot_0 && a[j*2+1]> pivot_1);
			if(i>=j)	break;
			Swap(ref a[i*2], ref a[j*2]);
			Swap(ref a[i*2+1], ref a[j*2+1]);
		}
		Swap (ref a [l * 2 + 0], ref a [j * 2 + 0]);
		Swap (ref a [l * 2 + 1], ref a [j * 2 + 1]);
		return j;
	}

	void Swap(ref int a, ref int b)
	{
		int temp = a;
		a = b;
		b = temp;
	}


	 void Strain_Limiting(){
		Mesh mesh = GetComponent<MeshFilter> ().mesh;
        Vector3[] vertices = mesh.vertices;
        Vector3[] sum_x = new Vector3[vertices.Length];
        int[] sum_N = new int[vertices.Length];
		Vector3[] temp = new Vector3[vertices.Length];
        float w = 0.2f;

        for (int i = 0; i < edge_list.Length / 2; i++){
			// find two vertices that make up an edge
			int index_i = edge_list[i * 2];
			int index_j = edge_list[i * 2 + 1];
			Vector3 xi = vertices[index_i];
			Vector3 xj = vertices[index_j];

			Vector3 xei = L0[i] * ((xi - xj) / (xi - xj).magnitude);
			xei = (xi + xj + xei);
			xei = new Vector3(xei.x * 0.5f, xei.y * 0.5f, xei.z * 0.5f);

			Vector3 xej = L0[i] * ((xj - xi) / (xj - xi).magnitude);
			xej = (xi + xj + xej);
			xej = new Vector3(xej.x * 0.5f, xej.y * 0.5f, xej.z * 0.5f);

			sum_x[index_i] += xei;
			sum_x[index_j] += xej;

			sum_N[index_i]++;
			sum_N[index_j]++;
        }

        for (int i = 0; i < vertices.Length; i++){
			if (i != 0 && i != 10){
				temp[i] = (w * vertices[i] + sum_x[i]) / (w + sum_N[i]); //average
				Vector3 v = velocities[i] + (temp[i] - vertices[i]) / t;
				velocities[i] = v;
			}
        }

		temp[0] = vertices[0];
		temp[10] = vertices[10];
		mesh.vertices = temp;
    } 


	void Collision_Handling()
	{
		Vector3[] vertices = GetComponent<MeshFilter>().mesh.vertices;
		GameObject sphere = GameObject.Find("Sphere");
		Vector3 center = sphere.transform.TransformPoint(0f, 0f, 0f);
		float radius = 5.2f;
		Mesh mesh = GetComponent<MeshFilter> ().mesh;	
		Vector3 temp = new Vector3(0, 0, 0);
		Vector3 point1 = vertices[0];
		Vector3 point2 = vertices[10];


		for(int i = 0; i < vertices.Length; i++){
			float distance = (float)Math.Pow(vertices[i].x - center.x, 2) + (float)Math.Pow(vertices[i].y - center.y, 2) + (float)Math.Pow(vertices[i].z - center.z, 2);

			if (Math.Pow(distance, 2) < Math.Pow(radius, 2)){
				temp = center + radius * ((vertices[i] - center) / (vertices[i] - center).magnitude);
				Vector3 v = velocities[i] + (temp - vertices[i]) / t;
				velocities[i] = v;
				temp += dt * velocities[i];
				vertices[i] = temp;
			}
		}
		
		vertices[0] = point1;
		vertices[10] = point2;
		//mesh.vertices = vertices;
	}

	// Update is called once per frame
	void Update () 
	{
		Mesh mesh = GetComponent<MeshFilter> ().mesh;
	    Vector3[] vertices = mesh.vertices;

		for (int i = 0; i < vertices.Length; i ++) {
			if(i != 0 && i != 10){
				// update velocity
				velocities[i] += t * new Vector3(0f, -9.8f, 0f);
				velocities[i] *= damping;
				//update position
				vertices[i] += t * velocities[i];
			}
		}

		mesh.vertices = vertices;
		mesh.RecalculateNormals ();

		for(int k = 0; k < 64; k++){
			Strain_Limiting();
		}	

		Collision_Handling();
	}
}
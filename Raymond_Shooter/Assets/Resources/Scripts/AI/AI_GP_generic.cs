using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
//isaimed
public class AI_GP_generic : MonoBehaviour {
    
    
    
    //evaluation stat
    float health_loss = 0;//health being taken off


    public struct gene
    {
        public neuron[][] neuro_network;
        public int generation;
        public float fitness;
        //{
        //    get { return fitness; }
        //    set { fitness = value; }
        //}

        //geno type
        public int gender;
        public float size;
        public void set_fitness(float fit)
        {
            fitness = fit;
        }
        public gene(neuron[][] neuro_network, int generation, float fitness, int gender, float size)
        {
            this.neuro_network = neuro_network;
            this.generation = generation;
            this.fitness = fitness;
            this.gender = gender;
            this.size = size;
        }
        public int CompareTo(gene other)
        {
            if(fitness == other.fitness)
            {
                return 0;
            }
            else if(fitness > other.fitness)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }
    }
    public class neuron
    {
        public float[] conn_weight;
        public float value;
    }
    public LayerMask enemyFilter;
    static float LOS_dist = 20;
    //public List<neuron>[] network_layers = new List<neuron>[3];
    //public List<List<neuron>[]> network_layers;
    //List<neuron[][]> gene_pool;
    List<gene> gene_pool;
    int generation = 0;
    public TextAsset gene_file;
    static int POPULATION = 20;
    static int N_HIDDEN_LAYERS = 2;
    static float MUTATION_CHANCE = 0.05f;
    static float BIRTH_REPLACEMENT_RATE = 0.3f;//best 0.6 of previous generation couple up to make the 0.3 children, while the 0.1 medium quality stays the same
    int generations = 0;
    public List<GameObject> active_testants = new List<GameObject>();
    private Dictionary<GameObject, int> testants_ID = new Dictionary<GameObject, int>();
    //sprivate List<KeyValuePair<GameObject, int>> testants_ID;
    private int target_gene_count = 0;//pointer for the first untested gene
    private float time_to_sense = 0;
    private float sensor_update_interval = 0.2f;

    
    // Use this for initialization
    void Start () {
        List<string> eachline = new List<string>();
        eachline.AddRange(gene_file.text.Split("\n"[0]));
        generation = int.Parse(eachline[0].Substring(12));
        Debug.Log("Generation: "+generation);
        if (eachline.Count <= 2)
        {
            Debug.Log("Empty file, creating a new gene pool");
            int success = initialize_random_gene();
            eachline.Clear();
            eachline.AddRange(gene_file.text.Split("\n"[0]));
        }
        else
        {
            Debug.Log("File read successful: ");
        }
        gene_pool = new List<gene>();//new List<neuron[][]>();
        Debug.Log("loading gene");
        bool reading = false;
        int gene_index = 0;
        int geno_index = 0;
        int layer_index = 0;
        for(int index = 0; index < eachline.Count; index++)
        {
            if (eachline[index].Contains("Stat"))
            {
                if(eachline[index][10] == '1')
                {
                    target_gene_count = gene_index;
                }
                layer_index = 0;
                geno_index = 0;
                List<string> param = new List<string>();
                param.AddRange(eachline[index].Split(" "[0]));
                gene_pool.Add(new gene(new neuron[N_HIDDEN_LAYERS + 2][], int.Parse(param[1]), float.Parse(param[3]), int.Parse(param[4]), float.Parse(param[5])));//adding a new gene unit
                Debug.Log("size: "+ float.Parse(param[5]));
                //Initialize input & hidden layer
                for (int layer = 0; layer < N_HIDDEN_LAYERS + 1; layer++)
                {
                    gene_pool[gene_index].neuro_network[layer] = new neuron[6];
                }
                //Initialize output layer
                gene_pool[gene_index].neuro_network[N_HIDDEN_LAYERS + 1] = new neuron[6];
                index += 2;
                reading = true;
            }
            else if (eachline[index].Contains("Layer:"))
            {
                layer_index++;
                geno_index = 0;
            }
            else if (reading)
            {
                List<string> connections = new List<string>();
                connections.AddRange(eachline[index].Split(" "[0]));
                //Debug.Log(gene_index+" "+ layer_index+" "+ geno_index);
                gene_pool[gene_index].neuro_network[layer_index][geno_index] = new neuron();
                int connection_count;
                if(layer_index != N_HIDDEN_LAYERS + 1)
                {
                    gene_pool[gene_index].neuro_network[layer_index][geno_index].conn_weight = new float[6];
                    connection_count = 6;
                }
                else
                {
                    gene_pool[gene_index].neuro_network[layer_index][geno_index].conn_weight = new float[3];
                    connection_count = 3;
                }

                for (int conn_index = 0; conn_index < connection_count; conn_index++)
                {
                    if(connections[conn_index].Length > 1 && !connections[conn_index].Contains(";"))
                    {
                        gene_pool[gene_index].neuro_network[layer_index][geno_index].conn_weight[conn_index] = float.Parse(connections[conn_index]);
                    }
                }
                geno_index++;
                if (eachline[index].Contains(";"))
                {
                    Debug.Log("gene end");
                    reading = false;
                    gene_index++;
                }
            }
        }
        Debug.Log("loading "+ gene_index + " gene candidates completed");


        //Wire testants
        //testants_ID
        Debug.Log("first unevaluated: "+ target_gene_count);
        
        if (active_testants.Count <= gene_pool.Count - target_gene_count)//enough gene pool to test for without changing generation
        {
            for(int i = 0; i < active_testants.Count; i++)
            {
                testants_ID.Add(active_testants[i], target_gene_count + i);
                active_testants[i].GetComponent<Body_generic>().OnDeathSubmitScore = submit_fitness_score;
                Debug.Log("pair: "+ active_testants[i] + " : "+(target_gene_count + i));
            }
        }
        else if(gene_pool.Count > target_gene_count)
        {
            for (int i = 0; i < gene_pool.Count - target_gene_count; i++)
            {
                testants_ID.Add(active_testants[i], target_gene_count + i);
                active_testants[i].GetComponent<Body_generic>().OnDeathSubmitScore = submit_fitness_score;
                Debug.Log("pair: " + active_testants[i] + " : " + (target_gene_count + i));
            }
            int allocated = gene_pool.Count - target_gene_count;
            breed_generation(target_gene_count - 1);
            target_gene_count = 0;
            for (int i = 0; i < active_testants.Count - allocated; i++)
            {
                testants_ID.Add(active_testants[allocated + i], allocated + i);
                active_testants[allocated + i].GetComponent<Body_generic>().OnDeathSubmitScore = submit_fitness_score;
                Debug.Log("pair: " + active_testants[allocated + i] + " : " + (allocated + i));
            }
        }
        else
        {
            breed_generation(gene_pool.Count);
            target_gene_count = 0;
            for (int i = 0; i < active_testants.Count; i++)
            {
                testants_ID.Add(active_testants[i], target_gene_count + i);
                active_testants[i].GetComponent<Body_generic>().OnDeathSubmitScore = submit_fitness_score;
                Debug.Log("pair: " + active_testants[i] + " : " + (target_gene_count + i));
            }
        }
        
    }
    void submit_fitness_score(KeyValuePair<GameObject, float> param)
    {
        
        gene test = new gene();
        test.fitness = 0;
        int gene_index = testants_ID[param.Key];
        gene_pool[gene_index].set_fitness(param.Value);//
    }
    void breed_generation(int upper_range)
    {
        target_gene_count = 0;
        generation++;
        Debug.Log("New generation: " + generation);
        List<gene> update_gene_pool = gene_pool.GetRange(0, upper_range);
        update_gene_pool.Sort(delegate (gene x, gene y){return x.fitness.CompareTo(y.fitness);});//increasing fitness
        int chosen_number = (int)(upper_range * BIRTH_REPLACEMENT_RATE) * 2;
        for (int i = 0; i < chosen_number; i+=2)
        {
            update_gene_pool[update_gene_pool.Count - 1 - i] = mate(update_gene_pool[i], update_gene_pool[i + 1]);
        }
        Debug.Log("gene size before1: " + gene_pool.Count);
        gene_pool.RemoveRange(0, upper_range);
        Debug.Log("gene size before2: " + gene_pool.Count);
        gene_pool.AddRange(update_gene_pool);
        Debug.Log("gene size after: " + gene_pool.Count);
        Debug.Log("Breeding End!");
    }
    gene mate(gene father, gene mother)
    {
        gene baby = new gene();
        baby.generation = Mathf.Max(father.generation, mother.generation) + 1;
        baby.gender = UnityEngine.Random.Range(0, 2);
        baby.size = (father.size + mother.size) / 2;
        //neuro crossover
        baby.neuro_network = new neuron[N_HIDDEN_LAYERS + 1][];
        int connections_count = N_HIDDEN_LAYERS * 6 * 6 + 3 * 6;
        int crossover = UnityEngine.Random.Range(1, connections_count);
        int father_first = UnityEngine.Random.Range(0, 2);
        int crossover_countdown = crossover;
        for (int i = 0; i < N_HIDDEN_LAYERS + 1; i++)//each layer
        {
            for(int j = 0; j < baby.neuro_network[i].Length; j++)//each neuron
            {
                for(int k = 0; k < baby.neuro_network[i][j].conn_weight.Length; k++)
                {
                    if((crossover_countdown > 0 && father_first == 1) || (crossover_countdown < 0 && father_first == 0))//father section
                    {
                        baby.neuro_network[i][j].conn_weight[k] = father.neuro_network[i][j].conn_weight[k];
                    }
                    else
                    {
                        baby.neuro_network[i][j].conn_weight[k] = mother.neuro_network[i][j].conn_weight[k];
                    }
                    crossover_countdown--;
                }
            }
        }
        return baby;
    }
    void Update()
    {
        if(Time.time > time_to_sense)
        {
            //time_to_sense = Time.time + sensor_update_interval;
            foreach(GameObject x in active_testants)
            {

                sense(x);
                
            }
        }
    }

    //Input constants
    int gender;
    float size;
    //Input sensors
    float sight_acc = 0;//the closer eye sight is to enemy and the more the enemies are, the higher this value would be
    float dist = -1;//closest enemy distance
    int enemies_insight = 0;
    int aimed = 0;

    //Initialize phase
    private int initialize_random_gene()
    {
        StreamWriter writer = new StreamWriter("Assets/Resources/Evolve_data.txt", true);
        if(writer == null)
        {
            return -1;
        }
        for (int i = 0; i < POPULATION; i++)
        {
            int gender = UnityEngine.Random.Range(0, 2);
            float body_size = UnityEngine.Random.Range(0.3f, 1.2f);
            writer.WriteLine("\nStat: 0 0 0 " + gender + " " + body_size + " #generation #evaluated #fitness #gender #size");
            writer.WriteLine("input layer 6 * 6");
            for(int x = 0; x <= N_HIDDEN_LAYERS; x++)//including input layer, excluding output layer
            {
                writer.WriteLine("Layer: " + x + "-"+(x+1));
                for (int j = 0; j < 6; j++)
                {
                    string line = "";
                    for (int k = 0; k < 6; k++)
                    {
                        line += UnityEngine.Random.Range(0.0f, 1.0f) + " ";
                    }
                    writer.WriteLine(line);
                }
            }
            writer.WriteLine("Layer: " + (N_HIDDEN_LAYERS) +"-Output layer");
            for (int j = 0; j < 6; j++)//output layer
            {
                string line = "";
                for (int k = 0; k < 3; k++)
                {
                    line += UnityEngine.Random.Range(0.0f, 1.0f) + " ";
                }
                if(j == 5)
                {
                    line += ";";
                }
                writer.WriteLine(line);
            }
        }
        writer.Close();
        Debug.Log("success");
        return 1;
    }
    //Input
    private void sense(GameObject target_AI)
    {
        float[] inputs = new float[6];
        float sight_acc = 0;
        float dist_acc = 0;
        int enemies_insight = 0;
        int aimed = 0;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(target_AI.transform.position, LOS_dist, enemyFilter);
        if(colliders != null)
        {
            enemies_insight = colliders.Length;
            for (int i = 0; i < colliders.Length; i++)
            {
                float ai_angle = target_AI.GetComponent<Rigidbody2D>().rotation;
                Vector2 enemy_dir_vec = (colliders[i].transform.position - target_AI.transform.position).normalized;
                float enemy_dir = Mathf.Atan2(enemy_dir_vec.y, enemy_dir_vec.x) * 180 / Mathf.PI;
                Vector2 ai_angle_vec = new Vector2(Mathf.Cos(ai_angle * Mathf.PI / 180), Mathf.Sin(ai_angle * Mathf.PI / 180)).normalized;
                if (ai_angle >= 0 && ai_angle >= enemy_dir && ai_angle - enemy_dir <= 180)//enemy on the right
                {
                    sight_acc += 180 - Vector2.Angle(ai_angle_vec, enemy_dir_vec);
                }
                else if (ai_angle >= 0 && ai_angle >= enemy_dir && ai_angle - enemy_dir >= 180)//enemy on the left
                {
                    sight_acc -= 180 - Vector2.Angle(ai_angle_vec, enemy_dir_vec);
                }
                else if (ai_angle >= 0 && ai_angle <= enemy_dir)//enemy on the left
                {
                    sight_acc -= 180 - Vector2.Angle(ai_angle_vec, enemy_dir_vec);
                }
                else if (ai_angle <= 0 && ai_angle >= enemy_dir)//enemy on the right
                {
                    sight_acc += 180 - Vector2.Angle(ai_angle_vec, enemy_dir_vec);
                }
                else if (ai_angle <= 0 && ai_angle <= enemy_dir && enemy_dir - ai_angle <= 180)//enemy on the left
                {
                    sight_acc -= 180 - Vector2.Angle(ai_angle_vec, enemy_dir_vec);
                }
                else//enemy on the right
                {
                    sight_acc += 180 - Vector2.Angle(ai_angle_vec, enemy_dir_vec);
                }
                


                float temp = Vector2.Distance(target_AI.transform.position, colliders[i].transform.position);
                if(temp < LOS_dist)
                {
                    dist_acc += LOS_dist - temp;
                }
            }
            if (colliders.Length > 0)
            {
                sight_acc /= 180;
            }
            
            dist_acc /= LOS_dist;
        }
        
        inputs[0] = gene_pool[testants_ID[target_AI]].gender;
        inputs[1] = gene_pool[testants_ID[target_AI]].size;
        inputs[2] = sight_acc;
        inputs[3] = dist_acc;
        inputs[4] = enemies_insight;
        //inputs[5] = target_AI.GetComponent<Body_generic>().isAimed;
        sensor_input(inputs, target_AI);
        
    }
    private void sensor_input(float[] inputs, GameObject target_AI)
    {
        //int gender;
        //float size;
        //float sight_acc
        //float dist
        //int enemies_insight
        //int aimed

        int target_gene_index = testants_ID[target_AI];

        //zero values & insert value
        for (int i = 0; i < N_HIDDEN_LAYERS + 2; i++)
        {
            for(int j = 0; j < gene_pool[target_gene_index].neuro_network[i].Length; j++)
            {
                if(i == 0)
                {
                    gene_pool[target_gene_index].neuro_network[0][j].value = inputs[j];
                    //Debug.Log();
                }
                else
                {
                    gene_pool[target_gene_index].neuro_network[i][j].value = 0;
                }
            }
        }
        //Debug.Log("test connection: "+ gene_pool[0].neuro_network.Length);
        //for each layer before the output layer
        for (int i = 0; i < N_HIDDEN_LAYERS + 1; i++)
        {
            neuron geno1 = gene_pool[target_gene_index].neuro_network[i][0];
            for (int k = 0; k < geno1.conn_weight.Length; k++)
            {
                gene_pool[target_gene_index].neuro_network[i + 1][k].value += geno1.conn_weight[k] * geno1.value;
            }
            neuron geno2 = gene_pool[target_gene_index].neuro_network[i][1];
            for (int k = 0; k < geno2.conn_weight.Length; k++)
            {
                gene_pool[target_gene_index].neuro_network[i + 1][k].value += geno2.conn_weight[k] * geno2.value;
            }
            neuron geno3 = gene_pool[target_gene_index].neuro_network[i][2];
            for (int k = 0; k < geno3.conn_weight.Length; k++)
            {
                gene_pool[target_gene_index].neuro_network[i + 1][k].value += geno3.conn_weight[k] * geno3.value;
            }
            neuron geno4 = gene_pool[target_gene_index].neuro_network[i][3];
            for (int k = 0; k < geno4.conn_weight.Length; k++)
            {
                gene_pool[target_gene_index].neuro_network[i + 1][k].value += geno4.conn_weight[k] * geno4.value;
            }
            neuron geno5 = gene_pool[target_gene_index].neuro_network[i][4];
            for (int k = 0; k < geno5.conn_weight.Length; k++)
            {
                gene_pool[target_gene_index].neuro_network[i + 1][k].value += geno5.conn_weight[k] * geno5.value;
            }
            neuron geno6 = gene_pool[target_gene_index].neuro_network[i][5];
            for (int k = 0; k < geno6.conn_weight.Length; k++)
            {
                gene_pool[target_gene_index].neuro_network[i + 1][k].value += geno6.conn_weight[k] * geno6.value;
            }
        }

        //output actions
        turn_to_face(gene_pool[target_gene_index].neuro_network[N_HIDDEN_LAYERS + 1][0].value, target_AI);
        move(gene_pool[target_gene_index].neuro_network[N_HIDDEN_LAYERS + 1][1].value, (int)inputs[0], inputs[1], target_AI);
        scream(gene_pool[target_gene_index].neuro_network[N_HIDDEN_LAYERS + 1][2].value, (int)inputs[0]);
    }
    
    //Output actions
    void turn_to_face(float offset, GameObject target_AI)//0.0 - 1.0 as in -180d - 180d
    {
        //Debug.Log("offset: " + target_AI + " : " + offset);
        target_AI.GetComponent<Rigidbody2D>().angularVelocity = offset * 5;
    }
    void move(float speed, int gender, float body_size, GameObject target_AI)//0.0 - 1.0 as in 0 - 8 if male, 0-5 if female
    {
        //Debug.Log("target: : "+ target_AI + " : " + speed);
        float angle = target_AI.GetComponent<Rigidbody2D>().rotation * Mathf.PI / 180;
        Vector2 aim_dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        target_AI.GetComponent<Rigidbody2D>().velocity = speed / 10 * aim_dir.normalized;
        if (gender == 0)//male
        {
            //speed * 8
        }
        else//female
        {
            //speed * 5
        }
    }
    void scream(float seconds, int gender)//0.0 - 1.0 as in 0 - 1second
    {

    }
}

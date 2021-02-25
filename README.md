# Neo4j GDS Embeddings With SPTAG

An attempt to use Neo4j Node Embeddings (via [GDS](https://neo4j.com/docs/graph-data-science/1.5/)) with [SPTAG](https://github.com/Microsoft/SPTAG)

## IMPORTANT NOTICE

To run _any_ of these samples, you will need to run in **x64** mode!!

### Current State

This is a project I'm using to test the use of Neo4j Embeddings (FastRP and GraphSAGE at the moment) with external tools, in
this case SPTAG (Space Partition Tree and Graph). 

* FastRP Embeddings against the Game of Thrones dataset can be created, indexed and queried then stored back into the DB.
* GraphSAGE against the Retail dataset can be created and indexed.
  * As of yet, querying isn't working. Working guess at the moment is the dimensionality of the Embeddings isn't in the right 
    structure for SPTAG to interpret

The code is messy at the moment, I'm primarily putting it into GH to prevent accidental data loss - but feel free to chip in 
if you want, it'll be changing a bit I imagine.

### Files

* Program.cs
  * This is where the application is run from. At the moment comment/uncomment the sample you want to run. I'll maybe tidy this :)
  * Key Parameters:
    * Dimension = the dimension of the Embeddings generated
    * K = the number of nearest neighbors to try to find

* Original.cs
  * This contains the [original demo](https://github.com/microsoft/SPTAG/blob/master/docs/GettingStart.md) code from SPTAG, which I have abstracted some of the magic strings 
    and numbers to make it clearer what the parameters being used are. 
  * There is no need for a Neo4j database to be in use to try this.
  * This also takes an `N` parameter - which is the number of embeddings to generate

* Neo4jVersion.cs
  * This contains Game of Thrones FastRP based code
  * The data set can be got from the [Game of Thrones Examples](https://github.com/neo4j-examples/game-of-thrones) repository
  * Neo4j database settings are at the top of the file (`consts`)
  * The _data_ needs to be in the DB, but the code will generate the Catalog and Model if needed.

* Neo4jVersionRetail.cs
  * The beginning of trying GraphSAGE against the Retail dataset (from the [GDS Retail Demo](https://github.com/AliciaFrame/GDS_Retail_Demo) code base).
  * The _data_ needs to be in the DB, but the code will generate the Catalog and Model if needed.
  * Neo4j database settings are at the top of the file (`consts`)

* SptagHelper.cs
  * An attempt to 'type safe' the parameters available to SPTAG - you can see it in use in the calls to the Index setup methods:
    * ``` 
      var idx = new AnnIndex(SptagHelper.Algorithms.Bkt, SptagHelper.ValueTypes.Float, dimension);
      idx.SetBuildParam(nameof(SptagHelper.Parameters.DistCalcMethod), SptagHelper.Parameters.DistCalcMethod.L2);
      ```
   * The intention is to allow you to pick from one of the `SptagHelper.Algorithms` rather than try to remember the correct string.
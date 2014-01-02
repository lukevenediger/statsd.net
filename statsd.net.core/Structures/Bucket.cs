using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using statsd.net.core.Messages;

namespace statsd.net.core.Structures
{
  public abstract class Bucket
  {
    public BucketType BucketType { get; private set; }
    public string RootNamespace { get; set; }
    public long Epoch { get; private set; }

    public Bucket(BucketType bucketType,
      long epoch,
      string rootNamespace = "")
    {
      BucketType = bucketType;
      Epoch = epoch;
      RootNamespace = rootNamespace;
    }

    public abstract void FeedTarget(ITargetBlock<GraphiteLine> target);

    public abstract GraphiteLine[] ToLines();

    public static Bucket Clone(Bucket bucket)
    {
      // Don't clone the bucket, just send back this reference since nobody
      // needs to modify the data anyways.
      return bucket;
    }
  }

  public abstract class Bucket<TItemType> : Bucket
  {
    public KeyValuePair<string, TItemType>[] Items { get; private set; }

    public Bucket(BucketType bucketType, 
      KeyValuePair<string, TItemType>[] items, 
      long epoch, 
      string rootNamespace = "")
      : base(bucketType, epoch, rootNamespace)
    {
      Items = items;
    }
  }
}

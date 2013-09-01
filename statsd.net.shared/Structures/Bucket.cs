using statsd.net.shared.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace statsd.net.shared.Structures
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

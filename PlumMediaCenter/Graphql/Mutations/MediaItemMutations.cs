using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using System;
using PlumMediaCenter.Graphql.InputGraphTypes;
using PlumMediaCenter.Graphql.GraphTypes;
using PlumMediaCenter.Business.Repositories;
using PlumMediaCenter.Models;
using System.Collections.Generic;
using PlumMediaCenter.Business.MetadataProcessing;

namespace PlumMediaCenter.Graphql.Mutations
{
    public class MediaItemMutations
    {
        public MediaItemMutations(
            MediaItemRepository mediaItemRepository,
            UserRepository userRepository
        )
        {
            this.MediaItemRepository = mediaItemRepository;
            this.UserRepository = userRepository;
        }
        MediaItemRepository MediaItemRepository;
        UserRepository UserRepository;

        public void Register(RootMutationGraphType mutation)
        {
            mutation.Field<BooleanGraphType>().Name("setMediaItemProgress")
                .Description("Set the current progress location of a media item (i.e. the player is playing it and is saving the current progress location")
                .Argument<NonNullGraphType<IntGraphType>>("mediaItemId", "The id of the media item")
                .Argument<NonNullGraphType<IntGraphType>>("seconds", "The number of seconds of progress")
                .ResolveAsync(async (ctx) =>
                {
                    var mediaItemId = ctx.GetArgument<int>("mediaItemId");
                    var seconds = ctx.GetArgument<int>("seconds");
                    await this.MediaItemRepository.SetProgress(this.UserRepository.CurrentProfileId, mediaItemId, seconds);
                    return true;
                }
            );
        }
    }
}
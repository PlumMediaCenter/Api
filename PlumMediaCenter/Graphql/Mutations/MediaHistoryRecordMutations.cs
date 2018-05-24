
using GraphQL.Types;
using PlumMediaCenter.Business.Repositories;

namespace PlumMediaCenter.Graphql.Mutations
{
    public class MediaHistoryRecordMutations
    {

        public MediaHistoryRecordMutations(
            MediaRepository mediaRepository
        )
        {
            this.MediaRepository = mediaRepository;
        }
        MediaRepository MediaRepository;

        public void Register(RootMutationGraphType mutation)
        {
            mutation.Field<BooleanGraphType>().Name("deleteMediaHistoryRecord")
                .Description("Delete a media history record")
                .Argument<IntGraphType>("id", "The id of the media history record to delete")
                .ResolveAsync(async (ctx) =>
                {
                    var id = ctx.GetArgument<int>("id");
                    await this.MediaRepository.DeleteHistoryRecord(id);
                    return true;
                });
        }
    }
}
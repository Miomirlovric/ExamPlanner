using Application.Storage;

namespace Application.ExternalApi.Adapters;

public interface IAnswerAdapter<in TApiResult>
{
    GenericQuestionAnswers Adapt(TApiResult apiResult);
}

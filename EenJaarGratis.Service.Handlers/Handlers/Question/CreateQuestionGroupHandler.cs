﻿using AutoMapper;
using ClassLibrary1EenJaarGratis.Service.Storage;
using EenJaarGratis.Service.Storage;
using EenJaarGratis.Service.Storage.Domain;
using EenJaarGratis.Services.Handlers.Requests.Question;
using EenJaarGratis.Services.Handlers.Responses.Question;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EenJaarGratis.Services.Handlers.Handlers.Question;

public class CreateQuestionGroupHandler : IRequestHandler<CreateQuestionGroupRequest, QuestionGroupResponse>
{
    private readonly IMapper _mapper;
    private readonly IQuestionRepository _questionRepository;
    private readonly IPlayerRepository _playerRepository;

    public CreateQuestionGroupHandler(IMapper mapper, IQuestionRepository questionRepository, IPlayerRepository playerRepository)
    {
        _mapper = mapper;
        _questionRepository = questionRepository;
        _playerRepository = playerRepository;
    }

    public async Task<QuestionGroupResponse> Handle(CreateQuestionGroupRequest request, CancellationToken cancellationToken)
    {
        Service.Storage.Domain.Question question = await _questionRepository.GetById(request.QuestionId, query => query.Include(q => q.QuestionGroups));
        
        QuestionGroup questionGroup = QuestionGroup.Create(question, new List<Service.Storage.Domain.Player>());
        question.QuestionGroups.Add(questionGroup);

        await _questionRepository.Update(question, cancellationToken);
        return _mapper.Map<QuestionGroupResponse>(questionGroup);
    }
}
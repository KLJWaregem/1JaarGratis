﻿using MediatR;

namespace EenJaarGratis.Services.Handlers.Requests.Question;

public class DeleteQuestionRequest : IRequest
{
    public int Id { get; set; }
}
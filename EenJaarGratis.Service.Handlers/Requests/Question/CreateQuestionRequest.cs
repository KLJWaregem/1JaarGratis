﻿using EenJaarGratis.Services.Handlers.Responses.Question;
using MediatR;

namespace EenJaarGratis.Services.Handlers.Requests.Question;

public class CreateQuestionRequest : IRequest<QuestionResponse>
{
    public string Question { get; set; } = null!;
    public string Answer1 { get; set; } = null!;
    public string Answer2 { get; set; } = null!;
    public string Answer3 { get; set; } = null!;
    public int CorrectAnswer { get; set; }
}
﻿using EenJaarGratis.Service.Handlers.Responses;
using MediatR;

namespace EenJaarGratis.Service.Handlers.Requests;

public class CreatePlayerRequest : IRequest<PlayerResponse>
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
}
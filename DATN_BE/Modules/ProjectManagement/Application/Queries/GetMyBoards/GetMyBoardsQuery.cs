using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetMyBoards;

public record GetMyBoardsQuery : IRequest<MyBoardsDto>;

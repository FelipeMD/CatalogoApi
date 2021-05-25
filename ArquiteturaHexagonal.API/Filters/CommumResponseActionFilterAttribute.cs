using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using ANM.Core;
using ANM.Core.Exceptions;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SINOP.API.ViewModel;

namespace SINOP.API.Filters
{
    public class ComumResponseActionFilterAttribute : ActionFilterAttribute
    {
        private const string _erroResultMessager = "Ocorreu um erro inesperado. Tente novamente mais tarde. {0}";
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ComumResponseActionFilterAttribute> _logger;
        private readonly IMapper _mapper;

        public ComumResponseActionFilterAttribute(IMapper mapper, IWebHostEnvironment env,
            ILogger<ComumResponseActionFilterAttribute> logger)
        {
            this._mapper = mapper;
            this._env = env;
            this._logger = logger;
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            Type resultType = context?.Result?.GetType();
            PropertyInfo valuePropertyInfo =
                resultType?.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
            object result = null;

            if (resultType != null && valuePropertyInfo != null)
            {
                result = valuePropertyInfo.GetValue(context.Result);
            }

            IResponse response;
            if (result is IResponse || context.Result is IResponse)
            {
                response = result as IResponse ?? context.Result as IResponse;
                if (response.Success)
                {
                    this.ParseSuccessComumResponseViewModel(context, response);
                }
                else
                {
                    this.ParseExceptionComumResponseViewModel(context, response);
                }
            }

            base.OnResultExecuting(context);
        }

        public void ParseExceptionComumResponseViewModel(ResultExecutingContext context, IResponse response)
        {
            if (response.Exception is DomainException)
            {
                if (response.Exception is INotRollbackTransaction)
                {
                    context.Result = new OkObjectResult(new ComumResponseViewModel(true, response.Exception?.Message));
                }
                else if (response.Exception is ValidationResultDomainException exValidationResultDomainException)
                {
                    context.Result = new BadRequestObjectResult(exValidationResultDomainException.Validations);
                }
                else if (response.Exception is NotFoundDomainException exNotFoundDomainException)
                {
                    context.Result = new NotFoundObjectResult(exNotFoundDomainException.Message);
                }
                else
                {
                    context.Result = new BadRequestObjectResult(new[]
                        {new ValidationResult(response.Exception.Message, new[] {response.Exception.GetType().Name})});
                }
            }
            else
            {
                _logger.LogError(response.Exception, response.Exception.Message);
                if (!_env.IsDevelopment())
                {
                    var erroResultMessager = string.Format(_erroResultMessager, context.HttpContext.TraceIdentifier);
                    context.Result = new ObjectResult(new ComumResponseViewModel(false, erroResultMessager))
                    {
                        StatusCode = StatusCodes.Status500InternalServerError,
                    };
                }
                else
                {
                    context.Result = new BadRequestObjectResult(new[]
                        {new ValidationResult(response.Exception.Message, new[] {response.Exception.GetType().Name})});
                }
            }
        }

        public void ParseSuccessComumResponseViewModel(ResultExecutingContext context, IResponse response)
        {
            PropertyInfo responseGenericPropertyInfo = response?.GetType()
                .GetProperty(nameof(ComumResponseViewModel<int>.Data), BindingFlags.Public | BindingFlags.Instance);
            if (responseGenericPropertyInfo != null)
            {
                object responseGenericData = responseGenericPropertyInfo.GetValue(response);
                Type responseGenericDataType = responseGenericData.GetType();
                Type typeMapDestination = this.GetTypeToMapResult(context);

                ConstructorInfo responseGenericConstructorInfo = typeof(ComumResponseViewModel<>)
                    .MakeGenericType(typeMapDestination ?? responseGenericDataType)?.GetConstructor(new[]
                        {typeof(bool), typeMapDestination ?? responseGenericDataType, typeof(string)});

                if (typeMapDestination != null && typeMapDestination != responseGenericDataType)
                {
                    responseGenericData =
                        this._mapper.Map(responseGenericData, responseGenericDataType, typeMapDestination);
                }

                context.Result =
                    new OkObjectResult(responseGenericConstructorInfo.Invoke(new object[]
                        {true, responseGenericData, null}));
            }
            else
            {
                context.Result = new OkObjectResult(new ComumResponseViewModel(true));
            }
        }

        private Type GetTypeToMapResult(ResultExecutingContext context)
        {
            var reflectedActionDescriptor =
                context?.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
            Type typeToMapResult = null;

            if (reflectedActionDescriptor != null)
            {
                typeToMapResult = reflectedActionDescriptor
                    .MethodInfo
                    .GetCustomAttributes(typeof(ProducesResponseTypeAttribute), false)
                    .OfType<ProducesResponseTypeAttribute>()
                    .Where(w => w.StatusCode == StatusCodes.Status200OK)
                    .Select(s => s.Type.GenericTypeArguments.FirstOrDefault())
                    .FirstOrDefault();
            }

            return typeToMapResult;
        }
    }
}
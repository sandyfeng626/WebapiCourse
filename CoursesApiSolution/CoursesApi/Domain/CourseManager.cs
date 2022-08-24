using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace CoursesApi.Domain;

public class CourseManager
{

    private readonly MongoDbCoursesAdapter _adapter;
    private readonly HrApiAdapter _hrApiAdapter;
    private readonly FilterDefinition<CourseEntity> _filterAllCourses;
    private readonly ProjectionDefinition<CourseEntity, CourseDetailsResponse> _projectToCourseDetails;
    private readonly ProjectionDefinition<CourseEntity, CourseSummaryItemResponse> _projectToCourseSummary;
    private readonly Expression<Func<CourseEntity, CourseDetailsResponse>> _mapCourseToDetailsExpression;

    private readonly IOptions<DevelopmentFlags> _options;
    private readonly ILogger<CourseManager> _logger;

    private readonly Func<CourseEntity, CourseDetailsResponse> _mapCourseToDetailsFunc;
    public CourseManager(MongoDbCoursesAdapter adapter, IOptions<DevelopmentFlags> options, HrApiAdapter hrApiAdapter, ILogger<CourseManager> logger)
    {

        _adapter = adapter;
        _filterAllCourses = Builders<CourseEntity>.Filter.Where(c => c.IsRemoved == false);
        _mapCourseToDetailsExpression = (CourseEntity c) => new CourseDetailsResponse
        {
            Id = c.Id.ToString(),
            Title = c.Title,
            DeliveryLocation = c.DeliveryLocation,
            NumberOfHours = c.NumberOfHours
        };
        _projectToCourseDetails = Builders<CourseEntity>.Projection.Expression(_mapCourseToDetailsExpression);

        _projectToCourseSummary = Builders<CourseEntity>.Projection.Expression(c => new CourseSummaryItemResponse
        {
            Id = c.Id.ToString(),
            Title = c.Title
        });

        _mapCourseToDetailsFunc = _mapCourseToDetailsExpression.Compile();

        _options = options;
        _hrApiAdapter = hrApiAdapter;
        _logger = logger;
    }

    public async Task<CoursesResponse> GetAllCoursesAsync(CancellationToken ct)
    {
        var response = new CoursesResponse
        {
            Data = await _adapter.Courses
            .Find(_filterAllCourses)
            .Project(_projectToCourseSummary)
            .ToListAsync(ct)
        };

        return response;
    }

    private async Task<CourseDetailsResponse> AddCourseAsync(CourseCreateRequest request, DeliveryLocationTypes deliveryLocation)
    {
        // From A Model -> CourseEntity
        var courseToAdd = new CourseEntity
        {
            Title = request.Title,
            NumberOfHours = request.NumberOfHours,
            DeliveryLocation = deliveryLocation,
            IsRemoved = false,
            WhenCreated = DateTime.Now
        };

        await _adapter.Courses.InsertOneAsync(courseToAdd);
  
        return _mapCourseToDetailsFunc(courseToAdd);
    }

    public async Task<CourseDetailsResponse?> GetCourseByIdAsync(ObjectId courseId)
    {
        if(_options.Value.LogDataAccess)
        {
            // BAD: Don't do this, use ILogger
            Console.WriteLine("Logging some data access");
        }
            var byIdFilter = Builders<CourseEntity>.Filter.Where(c => c.Id == courseId);
            var filterByNotRemovedAndById = _filterAllCourses & byIdFilter;

        // TODO: Call the other API and ask it for the teacher for this course. Add that to the model.
            var response = await _adapter.Courses
                .Find(filterByNotRemovedAndById)
                .Project(_projectToCourseDetails)
                .SingleOrDefaultAsync();

        try
        {
            var instructor = await _hrApiAdapter.GetInstructorInfoForCourseAsync(courseId.ToString());
            response.Instructor = instructor;
        }
        catch (Exception ex)
        {

            // What is our plan b here?
            _logger.LogCritical("getting the instructor failed", ex);

            // corny plan b
            response.Instructor = new InstructorInfo
            {
                Name = "No Instructor Found, or has not been assigned",
                EmailAddress = "helpdesk@hypertheory.com"
            };


        }
        return response;
       
    }

    public async Task<CourseDetailsResponse> AddOnlineCourseAsync(CourseCreateRequest request)
    {
        return await AddCourseAsync(request, DeliveryLocationTypes.Online);
    }

    public async Task<CourseDetailsResponse> AddOnPremCourseAsync(CourseCreateRequest request)
    {
        return await AddCourseAsync(request, DeliveryLocationTypes.OnPrem);
    }

    public async Task RemoveCourseByIdAsync(string courseId)
    {
       if(ObjectId.TryParse(courseId, out var id))
        {
            var update = Builders<CourseEntity>.Update.Set(c => c.IsRemoved, true);
            var byIdFilter = Builders<CourseEntity>.Filter.Where(c => c.Id == id);
            var filterByNotRemovedAndById = _filterAllCourses & byIdFilter;

            await _adapter.Courses.UpdateOneAsync(filterByNotRemovedAndById, update);
        }
    }
}

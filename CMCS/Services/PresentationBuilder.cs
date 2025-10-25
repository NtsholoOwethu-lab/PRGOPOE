using CMCS.Data;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;
using System.Linq;

namespace CMCS.Services
{
    public static class PresentationBuilder
    {
        public static void BuildSimplePresentation(ApplicationDbContext db, string outputPath)
        {
            // gather simple stats
            var approvedCount = db.MonthlyClaims.Count(c => c.Status == Models.ClaimStatus.Approved);
            var rejectedCount = db.MonthlyClaims.Count(c => c.Status == Models.ClaimStatus.Rejected);
            var submittedCount = db.MonthlyClaims.Count();
            var totalPayout = db.MonthlyClaims.Where(c => c.Status == Models.ClaimStatus.Approved).Sum(c => (decimal?)c.TotalAmount) ?? 0m;

            // Create presentation
            using var presentation = PresentationDocument.Create(outputPath, PresentationDocumentType.Presentation);
            var presentationPart = presentation.AddPresentationPart();
            presentationPart.Presentation = new Presentation();

            var slideMasterPart = presentationPart.AddNewPart<SlideMasterPart>();
            slideMasterPart.SlideMaster = new SlideMaster(new CommonSlideData(new ShapeTree()));
            slideMasterPart.SlideMaster.Save();

            var slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>();
            slideLayoutPart.SlideLayout = new SlideLayout(new CommonSlideData(new ShapeTree()));
            slideLayoutPart.SlideLayout.Save();

            var slideIdList = presentationPart.Presentation.AppendChild(new SlideIdList());

            // helper to add a slide with plain paragraph text
            int slideIndex = 256;
            void AddSlide(string title, string[] paragraphs)
            {
                var slidePart = presentationPart.AddNewPart<SlidePart>();
                var sld = new Slide(new CommonSlideData(new ShapeTree()));
                // Title shape
                var titleShape = new P.Shape();
                titleShape.NonVisualShapeProperties = new NonVisualShapeProperties(new NonVisualDrawingProperties { Id = (UInt32Value)1U, Name = "Title" }, new NonVisualShapeDrawingProperties(new P.ShapeLocks { NoGrouping = true }));
                titleShape.ShapeProperties = new ShapeProperties();
                var txBody = new TextBody(new A.BodyProperties(), new A.ListStyle());
                var p = new A.Paragraph(new A.Run(new A.Text(title)));
                txBody.AppendChild(p);
                titleShape.AppendChild(txBody);
                sld.CommonSlideData.ShapeTree.AppendChild(titleShape);

                // content shape
                var contentShape = new P.Shape();
                contentShape.NonVisualShapeProperties = new NonVisualShapeProperties(new NonVisualDrawingProperties { Id = (UInt32Value)2U, Name = "Content" }, new NonVisualShapeDrawingProperties(new P.ShapeLocks { NoGrouping = true }));
                contentShape.ShapeProperties = new ShapeProperties();
                var txBody2 = new TextBody(new A.BodyProperties(), new A.ListStyle());
                foreach (var para in paragraphs)
                {
                    var run = new A.Run(new A.Text(para));
                    var paraObj = new A.Paragraph();
                    paraObj.Append(run);
                    txBody2.Append(paraObj);
                }
                contentShape.Append(txBody2);
                sld.CommonSlideData.ShapeTree.AppendChild(contentShape);

                slidePart.Slide = sld;
                slidePart.Slide.Save();

                var slideId = new SlideId { Id = (UInt32Value)slideIndex++, RelationshipId = presentationPart.GetIdOfPart(slidePart) };
                slideIdList.Append(slideId);
            }

            AddSlide("CMCS Summary", new[] {
                $"Generated: {DateTime.Now:yyyy-MM-dd}",
                $"Total claims: {submittedCount}",
                $"Approved: {approvedCount}",
                $"Rejected: {rejectedCount}",
                $"Total payout (approved): R {totalPayout:N2}"
            });

            AddSlide("Highlights", new[] {
                "• Exported from CMCS",
                "• Use the HR report for detailed claim rows",
                "• This is a lightweight auto-generated summary"
            });

            presentationPart.Presentation.Save();
        }
    }
}
